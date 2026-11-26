using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerCollision collision;
    private InputSystem_Actions inputActions;

    #region Input Actions
    //Move Actions
    private Vector2 move;
    //Jump Actions
    private bool jump;
    private bool jumpPressed;
    private bool jumpReleased;
    //Attack Actions
    private bool attack;
    private bool attackPressed;
    private bool attackReleased;
    //Dash Actions
    private bool dash;
    private bool dashPressed;
    private bool dashReleased;
    //Pause Actions
    private bool pause;
    private bool pausePressed;
    private bool pauseReleased;
    //Map Actions
    private bool map;
    private bool mapPressed;
    private bool mapReleased;
    //Item Actions
    private bool items;
    private bool itemsPressed;
    private bool itemsReleased;
    //Confirm Actions
    private bool confirm;
    private bool confirmPressed;
    private bool confirmReleased;
    //Cancel Actions
    private bool cancel;
    private bool cancelPressed;
    private bool cancelReleased;
    #endregion



    private Vector2 velocity;
    
    
    private float walkSpeed;
    private float walkAcceleration;
    private float walkDeceleration;
    private float walkTurnAcceleration;
    private float airTurnAcceleration;

    private float jumpBuffer;
    private float jumpBufferTimer;
    private float fallBuffer;
    private float fallBufferTimer;
    private float coyoteTime;
    private float coyoteTimeTimer;
    private bool isJumping;
    public float jumpForceStrong;
    public float jumpForceWeak;
    private float jumpReduction;

    private float fallSpeed;
    private float fallStrong;
    private float fallWeak;
    private float gravity;

    private bool grounded;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        collision = GetComponent<PlayerCollision>();
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }
    private void Start()
    {
        walkSpeed = 10f;
        walkAcceleration = 30f;
        walkDeceleration = 30f;
        walkTurnAcceleration = 60f;
        airTurnAcceleration = 30f;

        jumpBuffer = 0.1f;
        fallBuffer = 0.1f;
        coyoteTime = 0.1f;
        isJumping = false;
        jumpForceStrong = 21.79f;
        jumpForceWeak = 19.67f;
        jumpReduction = 0.5f;

        fallSpeed = 19f;
        fallStrong = 60f;
        fallWeak = 45f;

        gravity = fallStrong;
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        #region Get Inputs
        //Get Move Inputs
        move = inputActions.PlayerControls.Move.ReadValue<Vector2>();

        //Get Jump Inputs
        jump = inputActions.PlayerControls.Jump.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Jump.WasPressedThisDynamicUpdate()) { jumpPressed = true; }
        if (inputActions.PlayerControls.Jump.WasReleasedThisDynamicUpdate()) { jumpReleased = true; }

        //Get Attack
        attack = inputActions.PlayerControls.Attack.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Attack.WasPressedThisDynamicUpdate()) { attackPressed = true; }
        if (inputActions.PlayerControls.Attack.WasReleasedThisDynamicUpdate()) { attackReleased = true; }

        //Get Dash
        dash = inputActions.PlayerControls.Dash.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Dash.WasPressedThisDynamicUpdate()) { dashPressed = true; }
        if (inputActions.PlayerControls.Dash.WasReleasedThisDynamicUpdate()) { dashReleased = true; }

        //Get Pause
        pause = inputActions.PlayerControls.Pause.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Pause.WasPressedThisDynamicUpdate()) { pausePressed = true; }
        if (inputActions.PlayerControls.Pause.WasReleasedThisDynamicUpdate()) { pauseReleased = true; }

        //Get Map
        map = inputActions.PlayerControls.Map.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Map.WasPressedThisDynamicUpdate()) { mapPressed = true; }
        if (inputActions.PlayerControls.Map.WasReleasedThisDynamicUpdate()) { mapReleased = true; }

        //Get Item
        items = inputActions.PlayerControls.Items.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Items.WasPressedThisDynamicUpdate()) { itemsPressed = true; }
        if (inputActions.PlayerControls.Items.WasReleasedThisDynamicUpdate()) { itemsReleased = true; }

        //Get Confirm
        confirm = inputActions.PlayerControls.Confirm.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Confirm.WasPressedThisDynamicUpdate()) { confirmPressed = true; }
        if (inputActions.PlayerControls.Confirm.WasReleasedThisDynamicUpdate()) { confirmReleased = true; }

        //Get Cancel
        cancel = inputActions.PlayerControls.Cancel.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Cancel.WasPressedThisDynamicUpdate()) { cancelPressed = true; }
        if (inputActions.PlayerControls.Cancel.WasReleasedThisDynamicUpdate()) { cancelReleased = true; }
        #endregion
        #region Display Inputs
        if (false)
        {
            Debug.Log("Move: " + move);
            Debug.Log("Jump: " + jump + " Jump Pressed: " + jumpPressed + " Jump Released: " + jumpReleased);
            Debug.Log("Attack: " + attack + " Attack Pressed: " + attackPressed + " Attack Released: " + attackReleased);
            Debug.Log("Dash: " + dash + " Dash Pressed: " + dashPressed + " Dash Released: " + dashReleased);
            Debug.Log("Pause: " + pause + " Pause Pressed: " + pausePressed + " Pause Released: " + pauseReleased);
            Debug.Log("Map: " + map + " Map Pressed: " + mapPressed + " Map Released: " + mapReleased);
            Debug.Log("Items: " + items + " Items Pressed: " + itemsPressed + " Items Released: " + itemsReleased);
            Debug.Log("Confirm: " + confirm + " Confirm Pressed: " + confirmPressed + " Confirm Released: " + confirmReleased);
            Debug.Log("Cancel: " + cancel + " Cancel Pressed: " + cancelPressed + " Cancel Released: " + cancelReleased);
        }
        #endregion
    }

    private void FixedUpdate()
    {
        velocity = collision.GlobalToLocal(collision.externalVelocity);
        grounded = collision.detectionDown;


        //=====================================================================================================================================================
        //--------------------------------------------------------------------------------------------------------------- >>> WALKING, RUNNING, AND TURNING <<<
        //=====================================================================================================================================================

        float targetAcceration = 0;
        float targetSpeed = 0;

        //If not moving:
        //else do:
        if (move.x == 0)
        {
            targetSpeed = 0;
            targetAcceration = walkDeceleration;
        }
        else
        {
            targetSpeed = walkSpeed * Mathf.Sign(move.x);

            //If velocity exceeds walking speed:
            //else do:
            if (Mathf.Abs(velocity.x) > walkSpeed)
            {
                targetAcceration = walkDeceleration;
            }
            else
            {
                //If turning:
                //else do:
                if (move.x * velocity.x > 0)
                {
                    targetAcceration = walkAcceleration;
                }
                else
                {
                    //If grounded:
                    //else do:
                    if (grounded)
                    {
                        targetAcceration = walkTurnAcceleration;
                    }
                    else
                    {
                        targetAcceration = airTurnAcceleration;
                    }
                }
            }


            
        }

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, targetAcceration * Time.fixedDeltaTime);



        //=====================================================================================================================================================
        //------------------------------------------------------------------------------------------------------------------------- >>> JUMPING AND FALLING <<<
        //=====================================================================================================================================================

        //--------------------------------------
        //>> Timers and Cooldowns for Jumping <<
        //--------------------------------------

        #region Jump Timers
        //Jump Buffer
        if (jumpPressed)
        {
            jumpBufferTimer = jumpBuffer;
        }

        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.fixedDeltaTime;
        }

        //Fall Buffer
        if (jumpReleased)
        {
            fallBufferTimer = fallBuffer;
        }
        
        if (fallBufferTimer > 0)
        {
            fallBufferTimer -= Time.fixedDeltaTime;
        }

        //Coyote Time
        if (grounded)
        {
            coyoteTimeTimer = coyoteTime;
        }

        if (coyoteTimeTimer > 0)
        {
            coyoteTimeTimer -= Time.fixedDeltaTime;
        }
        #endregion


        

        float jumpForce = jumpForceStrong;


        if (coyoteTimeTimer > 0)
        {
            if (jumpBufferTimer > 0)
            {
                jumpBufferTimer = 0;
                coyoteTimeTimer = 0;
                isJumping = true;
                gravity = fallWeak;
                velocity.y = jumpForce;
                collision.groundSnap = false;
            }
            else
            {
                isJumping = false;
                gravity = fallStrong;
                collision.groundSnap = true;
            }
        }

        if ((fallBufferTimer > 0) && isJumping)
        {
            fallBufferTimer = 0;
            isJumping = false;
            gravity = fallStrong;
            if (velocity.y > 0)
            {
                velocity.y *= jumpReduction;
            }
        }

        if (!grounded)
        {
            velocity.y = Mathf.MoveTowards(velocity.y, -fallSpeed, gravity * Time.fixedDeltaTime);
        }

        //Debug.Log("Jump Buffer: " + jumpBufferTimer + " Coyote Timer: " + coyoteTimeTimer);
        Debug.Log("Fall Buffer: " + fallBufferTimer + " Jumping: " + isJumping);

        collision.externalVelocity = collision.LocalToGlobal(velocity);
        #region Reset Inputs
        jumpPressed = false;
        jumpReleased = false;
        attackPressed = false;
        attackReleased = false;
        dashPressed = false;
        dashReleased = false;
        pausePressed = false;
        pauseReleased = false;
        mapPressed = false;
        mapReleased = false;
        itemsPressed = false;
        itemsReleased = false;
        confirmPressed = false;
        confirmReleased = false;
        cancelPressed = false;
        cancelReleased = false;
        #endregion
    }
}
