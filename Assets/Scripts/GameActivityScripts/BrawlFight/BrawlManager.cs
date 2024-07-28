using UnityEngine;

public class BrawlManager : MonoBehaviour {

    [SerializeField] private GameObject arenaFloor;
    [SerializeField] private float characterSpeed;
    [SerializeField] private float jumpStrength;
    [SerializeField] private float smoothTime; // for character movement
    [SerializeField] private GameObject[] fighters;
    public float blockMoveSpeed;

    private PlayerBehavior[] fighterScript = new PlayerBehavior[2];
    private Rigidbody2D[] fighterRb = new Rigidbody2D[2];
    private BoxCollider2D[] fighterColliders = new BoxCollider2D[2];
    private GameObject pacman;
    private GameObject ghost;

    public GameObject getArenaFloor() { return arenaFloor; }
    public float getCharacterSpeed() { return characterSpeed; }
    public float getJumpStrength() { return jumpStrength; }
    public float getSmoothTime() { return smoothTime; }
    public GameObject[] getFighters() { return fighters; }
    public PlayerBehavior[] getFighterScript() { return fighterScript; }
    public Rigidbody2D[] getFighterRb() { return fighterRb; }
    public BoxCollider2D[] getFighterCollider() { return fighterColliders; }

    private void Start() {
        for (int i = 0; i < fighters.Length; i++) {
            GameObject fighter = fighters[i];

            fighterScript[i] = fighter.GetComponent<PlayerBehavior>();
            fighterRb[i] = fighter.GetComponent<Rigidbody2D>();
            fighterColliders[i] = fighter.GetComponent<BoxCollider2D>();
        }

        pacman = fighters[0];
        ghost = fighters[1];
    }

    private void Update() {

        // Make sprites always face opponent
        if (pacman.transform.position.x < ghost.transform.position.x) {
            if (fighterScript[0].facingRight) { return; }
            
            fighterScript[0].facingRight = true;
            fighterScript[1].facingRight = false;
            faceRight(pacman);
            faceLeft(ghost);

            // Debug.Log("Facing Left");
        } else {
            if (fighterScript[1].facingRight) { return; }

            fighterScript[0].facingRight = false;
            fighterScript[1].facingRight = true;
            faceRight(ghost);
            faceLeft(pacman);

            // Debug.Log("Facing Right");
        }
    }

    private void faceRight(GameObject fighter) {
        SpriteRenderer spriteRenderer = fighter.GetComponent<SpriteRenderer>();
        Transform attackBox = fighter.transform.GetChild(0);

        spriteRenderer.flipX = false;
        attackBox.localScale = new Vector3(1, 1, 1);
    }

    private void faceLeft(GameObject fighter) {
        SpriteRenderer spriteRenderer = fighter.GetComponent<SpriteRenderer>();
        Transform attackBox = fighter.transform.GetChild(0);

        spriteRenderer.flipX = true;
        attackBox.localScale = new Vector3(-1, 1, 1);
    }
}
