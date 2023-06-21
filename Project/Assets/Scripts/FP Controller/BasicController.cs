using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BasicController : MonoBehaviour
{
    PlayerControls controls;

    public CharacterController characterController = null;
    public Camera playerCamera = null;

    public GameObject Flashlight = null;

    public Vector2 inputView;

    private bool W = false;
    private bool A = false;
    private bool S = false;
    private bool D = false;
    private bool Jump = false;
    private bool jumping = false;
    private bool FlashlightToggle = false;
    private bool isFlashlightEnabled = false;

    public float playerSpeed = 8.0f;
    public float gravity = -20f;
    public float mouseSensitivity = 200.0f;
    public float jumpPower = 0.04f;
    public float jumpTime = 0.15f;
    public float jumpTimer = 0.0f;

    public LayerMask groundMask;
    bool isGrounded = false;

    public Transform playerBody;

    private Vector2 cameraRotation = Vector2.zero;
    private Vector3 horizontalVelocity = Vector3.zero;
    private Vector3 verticalVelocity = Vector3.zero;

    private void Awake()
    {
        controls = new PlayerControls();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        ToggleFlashlight(false);
    }

    private void Update()
    {
        ApplyInputs();
        ApplyGravity();
    }

    private void GetInputs()
    {
        controls.Gameplay.W.performed += ctx => W = true;
        controls.Gameplay.W.canceled += ctx => W = false;

        controls.Gameplay.A.performed += ctx => A = true;
        controls.Gameplay.A.canceled += ctx => A = false;

        controls.Gameplay.S.performed += ctx => S = true;
        controls.Gameplay.S.canceled += ctx => S = false;

        controls.Gameplay.D.performed += ctx => D = true;
        controls.Gameplay.D.canceled += ctx => D = false;

        controls.Gameplay.Jump.performed += ctx => Jump = true;
        controls.Gameplay.Jump.canceled += ctx => Jump = false;

        controls.Gameplay.Look.performed += ctx => inputView = ctx.ReadValue<Vector2>();

        controls.Gameplay.Flashlight.performed += ctx => FlashlightToggle = true;
    }

    private void ApplyInputs()
    {
        //WASD movement
        if (W)
        {
            horizontalVelocity = (transform.forward * playerSpeed);
            characterController.Move(horizontalVelocity * Time.deltaTime);
        }
        if (A)
        {
            horizontalVelocity = (transform.right * -playerSpeed);
            characterController.Move(horizontalVelocity * Time.deltaTime);
        }
        if (S)
        {
            horizontalVelocity = (transform.forward * -playerSpeed);
            characterController.Move(horizontalVelocity * Time.deltaTime);
        }
        if (D)
        {
            horizontalVelocity = (transform.right * playerSpeed);
            characterController.Move(horizontalVelocity * Time.deltaTime);
        }


        //mouse look
        float mouseX = inputView.x * mouseSensitivity * Time.deltaTime;
        float mouseY = inputView.y * mouseSensitivity * Time.deltaTime;

        cameraRotation.x -= mouseY;
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -80.0f, 80.0f);
        cameraRotation.y += mouseX;

        playerCamera.transform.localRotation = Quaternion.AngleAxis(cameraRotation.x, Vector3.right);
        transform.localRotation = Quaternion.AngleAxis(cameraRotation.y, Vector3.up);


        //jumping
        if (Jump && isGrounded && !jumping) jumping = true;

        if (jumping)
        {
            jumpTimer += Time.deltaTime;
            Vector3 jumpPowerv3 = new Vector3(0.0f, jumpPower, 0.0f);
            if (jumpTimer < jumpTime)
            {
                characterController.Move(jumpPowerv3);
            }
            else
            {
                jumping = false;
                jumpTimer = 0.0f;
            }
        }

        //Flashlight toggle
        if (FlashlightToggle)
        {
            FlashlightToggle = false;

            //discard ignores result of expression, but the isInventoryOpen value will be changed
            _ = isFlashlightEnabled == true ? isFlashlightEnabled = false : isFlashlightEnabled = true;

            ToggleFlashlight(isFlashlightEnabled);
        }
    }

    private void ApplyGravity()
    {
        if (!jumping)
        {
            Vector3 checkLocation = new Vector3(transform.position.x, transform.position.y - 1.0f, transform.position.z);
            isGrounded = Physics.CheckSphere(checkLocation, 0.3f, groundMask);
            if (isGrounded)
            {
                verticalVelocity.y = 0;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
                characterController.Move(verticalVelocity * Time.deltaTime);
            }
        }
    }

    private void ToggleFlashlight(bool enabled)
    {
        Flashlight.SetActive(enabled);
    }

    private void ToggleKeyInputs(bool enabled)
    {
        if (enabled)
        {
            controls.Gameplay.W.Enable();
            controls.Gameplay.A.Enable();
            controls.Gameplay.S.Enable();
            controls.Gameplay.D.Enable();
            controls.Gameplay.Jump.Enable();
            controls.Gameplay.Look.Enable();
        }
        else
        {
            controls.Gameplay.W.Disable();
            controls.Gameplay.A.Disable();
            controls.Gameplay.S.Disable();
            controls.Gameplay.D.Disable();
            controls.Gameplay.Jump.Disable();
            controls.Gameplay.Look.Disable();
            inputView = Vector2.zero;
        }
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
        GetInputs(); //call getinputs here to stop the no garbage collector problem
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }
}
