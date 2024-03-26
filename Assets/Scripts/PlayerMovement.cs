using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D coll;
    private SpriteRenderer sprite;
    private Animator anim;




    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Image dashIndicator;
    [SerializeField] private Image doubleJumpIndicator;





    private float dirX = 0f;
    private float wallJumpCooldown;
    private float vertical;
    private bool isLadder;
    private bool isClimbing;

    private bool canDoubleJump = false;

    private bool canDash = false;
    private bool isDashing;
    private float dashingPower = 24f;

    public TextMeshProUGUI stopwatchText;
    private float elapsedTime;
    private bool isRunning = false;


    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private TrailRenderer tr;







    private enum MovementState { idle, running, jumping, falling, walled }

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();


        StartStopwatch();

    }



    // Update is called once per frame
    private void Update()
    {


        if (Input.GetKey(KeyCode.Escape))

        {
            SceneManager.LoadSceneAsync(0);
        }

        if (isDashing)
        {
            return;
        }

        if (canDash && dashIndicator != null)
        {
            dashIndicator.gameObject.SetActive(true);
        }
        else if (dashIndicator != null)
        {
            dashIndicator.gameObject.SetActive(false);
        }


        if (canDoubleJump && doubleJumpIndicator != null)
        {
            doubleJumpIndicator.gameObject.SetActive(true);
        }
        else if (doubleJumpIndicator != null)
        {
            doubleJumpIndicator.gameObject.SetActive(false);
        }




        vertical = Input.GetAxis("Vertical");

        if (isLadder && Mathf.Abs(vertical) > 0f)
        {
            isClimbing = true;
        }

        dirX = Input.GetAxisRaw("Horizontal");



        if (Input.GetButtonDown("Dash") && canDash)

        {
            StartCoroutine(Dash());
        }


        if (wallJumpCooldown > 0.2f)
        {


            rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);

            if (IsWalled() && !IsGrounded())
            {
                rb.gravityScale = 0f;
                rb.velocity = Vector2.zero;
            }
            else
                rb.gravityScale = 3f;

            if (Input.GetButtonDown("Jump"))


                Jump();
        }
        else
            wallJumpCooldown += Time.deltaTime;






        UpdateAnimationState();

        
        if (isRunning)
        {
            UpdateStopwatch();
        }


    }



    private void Jump()
    {
        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

        }
        else if (IsWalled() && !IsGrounded())
        {
            wallJumpCooldown = 0f;

            float wallDirection = IsTouchingRightWall() ? -1f : 1f; // Determine which way to jump based on the wall side
            rb.velocity = new Vector2(wallDirection * moveSpeed, jumpForce);
       

        }

        else if (canDoubleJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            canDoubleJump = false; // Disable double jump until grounded again
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        // Determine the dash direction based on the player's facing direction
        float dashDirection = sprite.flipX ? -1f : 1f;

        // Apply dash velocity with corrected direction
        rb.velocity = new Vector2(dashDirection * dashingPower, 0f);

        tr.emitting = true;

        yield return new WaitForSeconds(0.3f);

        tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        canDash = false;
        


    }



    private void UpdateAnimationState()
    {

        MovementState state;

        if (IsWalled())
        {
            state = MovementState.walled;
            sprite.flipX = true;
        }

        if (dirX > 0f && !IsWalled())

        {
            state = MovementState.running;
            sprite.flipX = false;
        }
        else if (dirX < 0f && !IsWalled())

        {
            state = MovementState.running;
            sprite.flipX = true;
        }

        else
        {
            state = MovementState.idle;
        }

        if (rb.velocity.y > .1f && !IsWalled())
        {
            state = MovementState.jumping;

        }

        else if (rb.velocity.y < -.1f && !IsWalled())
        {
            state = MovementState.falling;
        }
        if (rb.velocity.y < -.1f && IsWalled())
        {
            state = MovementState.jumping;
            sprite.flipX = true;
        }

        anim.SetInteger("state", (int)state);
    }


    private bool IsGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, .1f, jumpableGround);
        return raycastHit.collider != null;

    }

    private bool IsWalled()
    {
        float wallDistance = 0.1f;
        float direction = transform.localScale.x > 0 ? 1 : -1;

        Vector2 rayOrigin = new Vector2(coll.bounds.center.x + (direction * coll.bounds.extents.x), coll.bounds.center.y);

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * direction, wallDistance, wallLayer);

        if (hit.collider == null)
        {
            // If we didn't hit a wall in the initial direction, check the opposite direction
            rayOrigin = new Vector2(coll.bounds.center.x - (direction * coll.bounds.extents.x), coll.bounds.center.y);
            hit = Physics2D.Raycast(rayOrigin, Vector2.right * -direction, wallDistance, wallLayer);
        }

        return hit.collider != null;

    }

    private bool IsTouchingRightWall()
    {
        float wallDistance = 0.1f;

        Vector2 rayOrigin = new Vector2(coll.bounds.center.x + coll.bounds.extents.x, coll.bounds.center.y);

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right, wallDistance, wallLayer);
        return hit.collider != null;
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            
            float dashDirection = sprite.flipX ? -1f : 1f;
            rb.velocity = new Vector2(dashDirection * dashingPower, 0f);
            return;
        }
        if (isClimbing)
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(rb.velocity.x, vertical * jumpForce);
        }
        else
        {
            rb.gravityScale = 3f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        var random = new System.Random();



        if (collision.CompareTag("Ladder"))
        {
            isLadder = true;

        }



        else if (collision.CompareTag("Item"))

        {
            int randomIndex = random.Next(2);


            if (randomIndex == 0)
            {
                canDoubleJump = true;
            }
            else
            {
                canDash = true;


            }


            
            Destroy(collision.gameObject);
        }

        if (collision.CompareTag("Goal"))

        {
            SceneManager.LoadSceneAsync(2);
            StopStopwatch();
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isLadder = false;
            isClimbing = false;
        }
    }

    void StartStopwatch()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    void StopStopwatch()
    {
        isRunning = false;
    }

    void UpdateStopwatch()
    {
        elapsedTime += Time.deltaTime;
        UpdateStopwatchUI();
    }

    void UpdateStopwatchUI()
    {
        if (stopwatchText != null)
        {
            stopwatchText.text = FormatTime(elapsedTime);
        }
    }


    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 1000) % 1000);

        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }




}