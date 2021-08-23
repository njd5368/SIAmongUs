using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

public class AU_PlayerController : MonoBehaviour, IPunObservable {
    //Fields
    [SerializeField] bool hasControl;
    public static AU_PlayerController localPlayer;
    Rigidbody auRigidbody;
    Transform avatar;
    Animator animator;

    float direction = 1;

    //Color Stuff
    static Color playerColor;
    SpriteRenderer spriteRenderer;

    [SerializeField] InputAction WASD;

    Vector2 inputMovement;
    [SerializeField] float movementSpeed;

    // Game Roles
    [SerializeField] bool isImposter;
    [SerializeField] InputAction KILL;

    List<AU_PlayerController> targets;
    [SerializeField] Collider auCollider;
    [SerializeField] GameObject bodyPrefab;

    bool isDead;

    public static List<Transform> allBodies;
    List<Transform> bodiesFound;

    [SerializeField] InputAction REPORT;
    [SerializeField] LayerMask ignoreForBody;

    // Interactions
    [SerializeField] InputAction MOUSE;
    Vector2 mousePositionInput;
    [SerializeField] Camera myCamera;
    [SerializeField] InputAction INTERACTION;
    [SerializeField] LayerMask interactionLayer;

    // Networking
    PhotonView myPV;
    [SerializeField] GameObject lightMask;
    [SerializeField] AU_LightCaster myLightCaster;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if(stream.IsWriting) {
            stream.SendNext(direction);
        }
        else {
            direction = (float)stream.ReceiveNext();
        }
    }

    private void Awake() {
        KILL.performed += KillTarget;
        REPORT.performed += ReportBody;
        INTERACTION.performed += Interact;
    }

    private void OnEnable() {
        WASD.Enable();
        KILL.Enable();
        REPORT.Enable();
        MOUSE.Enable();
        INTERACTION.Enable();
    }

    private void OnDisable() {
        WASD.Disable();
        KILL.Disable();
        REPORT.Disable();
        MOUSE.Disable();
        INTERACTION.Disable();
    }

    public void SetRole(bool isImposter) {
        this.isImposter = isImposter;
    }

    public void OnTriggerEnter(Collider other) {
        if(other.tag == "Player") {
            AU_PlayerController tmpTarget = other.GetComponent<AU_PlayerController>();
            if(isImposter && !tmpTarget.isImposter) {
                Debug.Log("Added to targets");
                targets.Add(tmpTarget);
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.tag == "Player") {
            AU_PlayerController tmpTarget = other.GetComponent<AU_PlayerController>();
            if(targets.Contains(tmpTarget)) {
                Debug.Log("Removed from targets");
                targets.Remove(tmpTarget);
            }
        }
    }

    // Kills the nearest crew member
    void KillTarget(InputAction.CallbackContext context) {
        Debug.Log("Kill attempted.");
        if(myPV.IsMine && isImposter && context.phase == InputActionPhase.Performed && targets.Count > 0) {
            //Order the list by the distance to the killer
            targets.Sort((entry1, entry2)=> Vector3.Distance(entry1.transform.position, transform.position).CompareTo(Vector3.Distance(entry2.transform.position, transform.position)));
            //Loop through the list and kill the nearest person who is alive.
            for(int i = 0; i < targets.Count; i++) {
                AU_PlayerController target = targets[i];
                if(!target.isDead) {
                    transform.position = target.transform.position;
                    target.myPV.RPC("RPC_Kill", RpcTarget.All);
                    Debug.Log("Send Kill Command");
                    break;
                }
            }
        }
    }

    [PunRPC]
    void RPC_Kill() {
        Debug.Log("Recieved Kill Command");
        Die();
    }

    public void Die() {
        if(myPV.IsMine) {
            isDead = true;
            animator.SetBool("isDead", isDead);
            auCollider.enabled = false;
            AU_Body tmpBody = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "AU Body"), transform.position, transform.rotation).GetComponent<AU_Body>();
            tmpBody.SetColor(spriteRenderer.color);
            gameObject.layer = 8;
            Debug.Log("Die complete");
        }
    }

    void BodySearch() {
        foreach(Transform body in allBodies) {
            RaycastHit hit;
            Ray ray = new Ray(transform.position, body.position -  transform.position);
            Debug.DrawRay(transform.position, body.position - transform.position, Color.cyan);
            if(Physics.Raycast(ray, out hit, 1000f, ~ignoreForBody)) {
                if(hit.transform == body) {
                    if(!bodiesFound.Contains(body.transform)) {
                        bodiesFound.Add(body.transform);
                    }
                }
                else {
                    bodiesFound.Remove(body.transform);
                }
            }
        }
    }

    private void ReportBody(InputAction.CallbackContext obj) {
        if(bodiesFound != null && bodiesFound.Count > 0) {
            bodiesFound.Sort((entry1, entry2)=> Vector3.Distance(entry1.position, transform.position).CompareTo(Vector3.Distance(entry2.position, transform.position)));
            Transform tmpBody = bodiesFound[0];
            allBodies.Remove(tmpBody);
            bodiesFound.Remove(tmpBody);
            tmpBody.GetComponent<AU_Body>().Report();
        }
    }

    private void Interact(InputAction.CallbackContext context) {
        if(context.phase == InputActionPhase.Performed) {
            RaycastHit hit;
            Ray ray = myCamera.ScreenPointToRay(mousePositionInput);
            if(Physics.Raycast(ray, out hit, interactionLayer) && hit.transform.tag == "Task") {
                Task tmp = hit.transform.GetComponent<Task>();
                tmp.PlayMiniGame();
            }
        }
    }

    // Start is called before the first frame update 
    void Start() {
        auRigidbody = GetComponent<Rigidbody>();
        avatar = transform.GetChild(0);
        animator = GetComponent<Animator>();
        spriteRenderer = avatar.GetComponent<SpriteRenderer>();
        targets = new List<AU_PlayerController>();

        myPV = GetComponent<PhotonView>();
        if(myPV.IsMine) {
            localPlayer = this;
            if(playerColor == Color.clear) {
                playerColor = Color.white;
            }
            spriteRenderer.color = playerColor;
            if(allBodies == null) {
                allBodies = new List<Transform>();
            }
            bodiesFound = new List<Transform>();
        }
        else {
            myCamera.gameObject.SetActive(false);
            lightMask.SetActive(false);
            myLightCaster.enabled = false;
        }
        
    }

    // Update is called once per frame
    void Update() {
        avatar.localScale = new Vector2(direction, 1);
        if(myPV.IsMine) {
            inputMovement = WASD.ReadValue<Vector2>();
            animator.SetFloat("Speed", inputMovement.magnitude);
            if(inputMovement.x != 0) {
                direction = Mathf.Sign(inputMovement.x);
            }
            if(allBodies.Count > 0) {
                BodySearch();
            }
            mousePositionInput = MOUSE.ReadValue<Vector2>();
        }
    }

    //Fixed update handles movement
    private void FixedUpdate() {
        if(myPV.IsMine) {
            auRigidbody.velocity = inputMovement * movementSpeed;
        }
    }

    public void SetColor(Color newColor) {
        playerColor = newColor;
        if(spriteRenderer != null) {
            spriteRenderer.color = playerColor;
        }
    }
}
