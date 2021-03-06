using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
	[Header("Shooting Properties")]
	public float fireRate = 0.25f;			//Cooldown before the next shot
    public float maxDistance = 20f;         //Maximum shooting distance
    public int bulletsPerRound = 4;        //Bullets per round
    public float reloadTime = 3f;           //Reload time
	public float accuracy = 1.5f;			//Enemy shooting accuracy

	[Header("Health")]
	public int health = 1;					//Enemy health

    private bool reloading;                 //Is the enemy reloading?
    private int bulletCounter;              //Bullet counter for rounds
    private float reloadCounter;            //Reload counter
	Animator enemyAnimator;   

	float nextFire = 0f;					//Variable to hold shoot cooldown
    public GameObject projectile;           //Projectile GameObject

    Vector2 bulletPosition;                 //Holds the bullet position
    public Vector2 bulletOffset = new Vector2(0.7f,0.7f); 	//Offset between the bullet and the player

	Collider2D enemyCollider;
    public GameObject player;
    private SpriteRenderer sprite;

	private BoxCollider2D playerCollider;
	private int penLayer;

    private int direction;

	void Start ()
	{
		//Get a reference to the required components
		enemyCollider = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
		playerCollider = player.GetComponent<BoxCollider2D>();
		penLayer = LayerMask.NameToLayer("PlayerPens");
        bulletCounter = 0;
        reloadCounter = 0f;
        direction = 1;
		enemyAnimator = GetComponent<Animator>();
	}

	void FixedUpdate()
	{
		enemyAnimator.SetBool("enemy_shooting",false);
		HandleShoot();
	}

	void HandleShoot()
	{
        //Can't shoot, reload
        if(!reloading && bulletCounter >= bulletsPerRound) {
            reloadCounter = Time.time + reloadTime;
            reloading = true;
			enemyAnimator.SetBool("enemy_shooting",false);
        }

        //Reload finished
        if(reloading && Time.time >= reloadCounter) {
            bulletCounter = 0;
            reloading = false;
        }

		Vector2 playerCenter = playerCollider.bounds.center;
        float distance = Vector3.Distance(playerCenter, transform.position);

		if (distance < maxDistance && Time.time > nextFire && !reloading) {
            nextFire = Time.time + fireRate;
			enemyAnimator.SetBool("enemy_shooting",true);
			SoundManager.PlaySound("shots");

			//Calculate shooting direction
            Vector3 shootingTarget = playerCenter;
			//Add random noise
			shootingTarget += (Vector3)Random.insideUnitCircle * accuracy;
			Vector2 bulletDirection = shootingTarget - transform.position; //Initial bullet direction
			
			//The player should flip if it's shooting in a diferent direction in relation to its current direction
			bool shouldFlip = (direction * bulletDirection.x) < 0;
			int playerDirection = shouldFlip ? -direction : direction;
            bulletPosition = transform.position;

			//Update bullet direction with the bullet offset
			bulletDirection = shootingTarget - (transform.position + new Vector3(bulletOffset.x * playerDirection, bulletOffset.y, 0));

			//Impossible shot (deadzone)
			//if((playerDirection * bulletDirection.x) < 0)
			//	return;

			//Detect overlap before instantiating
			Vector2 collisionPosition = new Vector2(bulletPosition.x + bulletOffset.x * playerDirection, bulletPosition.y + bulletOffset.y);
			Collider2D[] hitColliders = Physics2D.OverlapCircleAll(collisionPosition, Mathf.Abs(collisionPosition.x - enemyCollider.bounds.center.x) - enemyCollider.bounds.size.x / 2);

			foreach(Collider2D collider in hitColliders) {
				if(collider != null && collider != enemyCollider && !collider.isTrigger) {
					//Debug.Log(collider.ToString());
					return;
				}
			}

            bulletCounter++;
            
			GameObject clone = Instantiate(projectile, bulletPosition, Quaternion.identity) as GameObject;
            clone.layer = LayerMask.NameToLayer("EnemyBullets");
			Bullet bullet = clone.GetComponent<Bullet>();
			bullet.SetProperties(playerDirection, bulletOffset, bulletDirection,this.gameObject);
			//Debug.Log("Shot");

            if(shouldFlip) {
                transform.localScale = new Vector3(-1 * transform.localScale.x, transform.localScale.y, transform.localScale.z);
                direction *= -1;
            }
		}
    }

	void OnCollisionEnter2D(Collision2D collision)
	{
		if(collision.gameObject.layer == penLayer) {
			health--;
		}

		if(health <= 0) {
			Destroy(gameObject);
		}
	}
}
