using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{

	// Stairs check
	[Header("Player stairs check:")]
	[SerializeField] GameObject stepRayUpper;
	
	[SerializeField] GameObject stepRayLower;

	[SerializeField] float stepHeight = 0.3f;

	[SerializeField] float stepSmooth = 0.1f;




	// others
	[Header("Player UI:")]
	[SerializeField] Image healthbarImage;
	[SerializeField] GameObject ui;

	[SerializeField] GameObject cameraHolder;

	[Header("Player movement settings:")]

	[SerializeField] float mouseSensitivity;
	[SerializeField] float sprintSpeed;
	[SerializeField] float walkSpeed;
	[SerializeField] float jumpForce;
	[SerializeField] float smoothTime;

	[SerializeField] float shakeSpeed;
	[SerializeField] float shakeAmount;

	[Header("Player weapons:")]
	[SerializeField] Item[] items;
	float currentGunShotTime=0.0f;
	float pistolCoolDown = 0.0f;
	[SerializeField] float pistolRate;
	[SerializeField] float pistolRrecoil;

	[SerializeField] float ak47Rate;
	[SerializeField] float ak47Recoil;
	[SerializeField] float gunShakeAmount;
	[SerializeField] int ammoAmount = 300;
	[SerializeField] TMP_Text ammoAmountText;

	int itemIndex;
	int previousItemIndex = -1;

	float verticalLookRotation;
	bool grounded;
	Vector3 smoothMoveVelocity;
	Vector3 moveAmount;

	AudioSource getHitAudio;


	Rigidbody rb;

	PhotonView PV;

	const float maxHealth = 100f;
	float currentHealth = maxHealth;

	PlayerManager playerManager;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		PV = GetComponent<PhotonView>();

		playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();

		stepRayUpper.transform.position = new Vector3(stepRayUpper.transform.position.x, stepHeight, stepRayUpper.transform.position.z);

		getHitAudio = GetComponent<AudioSource>();
		
	}

	void Start()
	{
		if(PV.IsMine)
		{
			EquipItem(0);
		}
		else
		{
			Destroy(GetComponentInChildren<Camera>().gameObject);
			Destroy(rb);
			Destroy(ui);
		}
	}

	void Update()
	{
		if(!PV.IsMine)
			return;

		Look();
		Move();
		Jump();

		for(int i = 0; i < items.Length; i++)
		{
			if(Input.GetKeyDown((i + 1).ToString()))
			{
				EquipItem(i);
				break;
			}
		}

		if(Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
		{
			if(itemIndex >= items.Length - 1)
			{
				EquipItem(0);
			}
			else
			{
				EquipItem(itemIndex + 1);
			}
		}
		else if(Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
		{
			if(itemIndex <= 0)
			{
				EquipItem(items.Length - 1);
			}
			else
			{
				EquipItem(itemIndex - 1);
			}
		}

		if(Input.GetMouseButton(0) && itemIndex==0)
		{
			if(currentGunShotTime >= ak47Rate)
            {
				useGun(ak47Recoil);
			}
			else
            {
				currentGunShotTime += Time.deltaTime;
            }
		}
		else if(Input.GetMouseButtonDown(0) && itemIndex == 1 && pistolCoolDown>=0.35f)
        {
			useGun(pistolRrecoil);
			pistolCoolDown = 0.0f;
		}
		else
        {
			currentGunShotTime = 0.0f;
			if(itemIndex == 1)
            {
				pistolCoolDown += Time.deltaTime;
			}
        }

		if(transform.position.y < -10f) // Die if you fall out of the world
		{
			Die();
		}
	}

	void useGun(float recoil)
    {
		if(ammoAmount > 0)
        {
			items[itemIndex].Use();
			ammoAmount--;
			currentGunShotTime = 0.0f;
			gunShake();
			verticalLookRotation += recoil * mouseSensitivity;
			verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
			ammoAmountText.text = ammoAmount.ToString();
			cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
		}
	}

	void gunShake()
	{
		rb.AddForce(transform.up * gunShakeAmount);
	}

	void Look()
	{
		transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

		verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
		verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

		cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
	}

	void Move()
	{
		Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

		moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
		
		if((Input.GetAxisRaw("Horizontal") != 0.0f || Input.GetAxisRaw("Vertical") != 0.0f) && grounded)
        {
			shake();
        }
	}

	void shake()
    {
		rb.AddForce(transform.up * shakeAmount);
    }

	void Jump()
	{
		if(Input.GetKeyDown(KeyCode.Space) && grounded)
		{
			rb.AddForce(transform.up * jumpForce);
		}
	}

	void EquipItem(int _index)
	{
		if(_index == previousItemIndex)
			return;

		itemIndex = _index;

		items[itemIndex].itemGameObject.SetActive(true);

		if(previousItemIndex != -1)
		{
			items[previousItemIndex].itemGameObject.SetActive(false);
		}

		previousItemIndex = itemIndex;

		if(PV.IsMine)
		{
			Hashtable hash = new Hashtable();
			hash.Add("itemIndex", itemIndex);
			PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
		}
	}

	public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
		if(changedProps.ContainsKey("itemIndex") && !PV.IsMine && targetPlayer == PV.Owner)
		{
			EquipItem((int)changedProps["itemIndex"]);
		}
	}

	public void SetGroundedState(bool _grounded)
	{
		grounded = _grounded;
	}

	void FixedUpdate()
	{
		if(!PV.IsMine)
			return;

		rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);

		stepClimb();
	}

	public void TakeDamage(float damage)
	{
		PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage);
	}

	[PunRPC]
	void RPC_TakeDamage(float damage, PhotonMessageInfo info)
	{
		getHitAudio.Play();
		currentHealth -= damage;

		healthbarImage.fillAmount = currentHealth / maxHealth;

		if(currentHealth <= 0)
		{
			Die();
			PlayerManager.Find(info.Sender).GetKill();
		}
	}

	void Die()
	{
		playerManager.Die();
	}



	void stepClimb()
    {
		RaycastHit hitLower;
		if(Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, 0.1f))
        {
			RaycastHit hitUpper;
			if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper, 0.2f))
            {
				rb.position -= new Vector3(0f, -stepSmooth, 0f);
            }
        }

		RaycastHit hitLower45;
		if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitLower45, 0.1f))
		{

			RaycastHit hitUpper45;
			if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitUpper45, 0.2f))
			{
				rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
			}
		}

		RaycastHit hitLowerMinus45;
		if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitLowerMinus45, 0.1f))
		{

			RaycastHit hitUpperMinus45;
			if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitUpperMinus45, 0.2f))
			{
				rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
			}
		}
	}




}