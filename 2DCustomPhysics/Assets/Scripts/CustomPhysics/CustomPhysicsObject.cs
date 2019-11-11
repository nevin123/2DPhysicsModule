using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class CustomPhysicsObject : MonoBehaviour
{
    // Input fields
    [Header("Physics Settings")]
    [SerializeField] private LayerMask _layerMask;
    [SerializeField][Range(2,15)] private int _horizontalRayCount = 2;
    [SerializeField][Range(2,15)] private int _verticalRayCount = 2;

    [SerializeField] private float _maxSlopeAngle = 45;

    [Header("Size Settings")]
    [SerializeField] private float _feetWidth;
    [SerializeField] private float _bodyWidth;
    [SerializeField] private float _fixSpeed = 1f;

    // Components
    private Rigidbody2D _rb;
    private BoxCollider2D _col;

    // Variables
    private const float _skinWidth = 0.025f;
    private float _colliderHeight;
    private float _correctedColliderHeight;

    protected Vector2 _velocity;
	protected CollisionInfo _collisionInfo;

    // Monobehaviour Methods
    private void Awake() {
        PhysicsMaterial2D fmat = new PhysicsMaterial2D("Smooth");
        fmat.friction = 0;
        fmat.bounciness = 0;

        _col = GetComponent<BoxCollider2D>();
        _rb = GetComponent<Rigidbody2D>();

        // Configure components
        _col.sharedMaterial = fmat;
        _col.isTrigger = false;
        _rb.sharedMaterial = fmat;
        _rb.simulated = true;
        _rb.gravityScale = 0;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        _rb.interpolation = RigidbodyInterpolation2D.None;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _colliderHeight = _col.size.y;

        StartCoroutine(AddColliderOffsetForSlopeAngle());
    }

    private void Start() {
		_collisionInfo = new CollisionInfo();

		Physics2D.queriesHitTriggers = false;
        _rb.gravityScale = 0;
    }

	private void FixedUpdate() {
		_collisionInfo.Reset();
		transform.rotation = Quaternion.identity;

        _velocity.x = Input.GetAxisRaw("Horizontal") * 4f;
        _velocity += Physics2D.gravity * Time.fixedDeltaTime;
        
        if(Input.GetButton("Jump")) _velocity.y = 10f;
		
        Vector2 deltaVelocity = _velocity * Time.deltaTime;

        if(_velocity.y > 0) CheckVerticalCollision(ref deltaVelocity, false);
        if(_velocity.y <= 0) CheckVerticalCollision(ref deltaVelocity, true);

		if (_collisionInfo.slidingDownSlope) Debug.Log("Sliding down slope");

        CheckHorizontalCollision(ref deltaVelocity, true);
        CheckHorizontalCollision(ref deltaVelocity, false);

		if (_collisionInfo.collidingBelow)
		{
			float velX = deltaVelocity.x;
			Vector2 velocityAlongGround = _collisionInfo.groundNormal * velX;
			deltaVelocity.x = 0;
			deltaVelocity += velocityAlongGround;
		}

		// rb.MovePosition(rb.position + deltaVelocity);
		_rb.velocity = deltaVelocity / Time.fixedDeltaTime;
    }

	// Private Methods
	private IEnumerator AddColliderOffsetForSlopeAngle()
	{
		if (_correctedColliderHeight == 0)
		{
			while (_colliderHeight == _col.size.y)
			{
				yield return new WaitForEndOfFrame();
			}
			_correctedColliderHeight = _col.size.y;
		}

		float colliderOffsetPerDegree = 0.00025f;
		float colliderOffset = _maxSlopeAngle * colliderOffsetPerDegree / transform.lossyScale.y;

		_col.size = new Vector2(_col.size.x, _correctedColliderHeight - colliderOffset);
		_col.offset = new Vector2(0, _col.offset.y + colliderOffset / 2f);
	}

	private void CheckHorizontalCollision(ref Vector2 refVelocity, bool left) {
        Vector3 startPosition = transform.position + transform.up * _skinWidth + (left ? -transform.right : transform.right) * (_feetWidth/2f - _skinWidth);
        float rayLength = (_bodyWidth - _feetWidth) / 2f + 2 *_skinWidth;
        
        if ((left && refVelocity.x < 0) || (!left && refVelocity.x > 0)) {
            rayLength += Mathf.Abs(refVelocity.x);
        }

        for (int i = 0; i < _horizontalRayCount; i++)
        {
            Vector3 rayStartPos = startPosition + transform.up * (transform.lossyScale.y * _colliderHeight - _skinWidth * 2f)/(_horizontalRayCount-1) * i;
            RaycastHit2D hit = Physics2D.Raycast(rayStartPos, left ? -transform.right : transform.right, rayLength, _layerMask);

            if (hit) {
                if(hit.distance == 0 && i == 0) continue;

                float hitAngle = Vector2.Angle(transform.up, hit.normal);
                if(hitAngle < _maxSlopeAngle) continue;

				// Set the body width
				float bodyWith = (_bodyWidth - _feetWidth) / 2f + _skinWidth;
				float anglePercentage = Mathf.Clamp01((hitAngle - _maxSlopeAngle) / (90f - _maxSlopeAngle));
				bodyWith -= Mathf.Lerp((_bodyWidth - _feetWidth) / 2f, 0, anglePercentage);

				// Limit the x velocity
				if (hit.distance >= bodyWith) {
                    refVelocity.x = left ? Mathf.Clamp(refVelocity.x, -hit.distance + bodyWith, float.MaxValue) : 
                                           Mathf.Clamp(refVelocity.x, float.MinValue, hit.distance - bodyWith);
                } else {
                    refVelocity.x = left ? Mathf.Clamp(refVelocity.x, Mathf.Lerp(0, -hit.distance + bodyWith, Time.fixedDeltaTime * _fixSpeed), float.MaxValue) : 
                                           Mathf.Clamp(refVelocity.x, float.MinValue, Mathf.Lerp(0, hit.distance - bodyWith, Time.fixedDeltaTime * _fixSpeed));
                }

				if (left) _collisionInfo.collidingLeft = true;
				if (!left) _collisionInfo.collidingRight = true;

				Debug.DrawLine(rayStartPos, rayStartPos + (left ? -transform.right : transform.right) * hit.distance, Color.red);
            } else {
                Debug.DrawLine(rayStartPos, rayStartPos + (left ? -transform.right : transform.right) * rayLength, Color.green);
            }
        }
    }

    private void CheckVerticalCollision(ref Vector2 refVelocity, bool bottom) {
        Vector3 startPosition = transform.position + transform.right * (_feetWidth/2f - _skinWidth) + transform.up * (bottom ? _skinWidth : _colliderHeight * transform.lossyScale.y - _skinWidth);
        float rayLength = 2 *_skinWidth;
        if ((bottom && refVelocity.y <= 0) || (!bottom && refVelocity.y > 0)) {
            rayLength += Mathf.Abs(refVelocity.y);
        }

		// Check if center is grounded
		if (bottom)
		{
			RaycastHit2D groundHit = Physics2D.Raycast(transform.position + transform.up * _skinWidth, -transform.up, rayLength, _layerMask);
			if (groundHit)
			{
				Vector2 groundNormal = new Vector2(groundHit.normal.y, -groundHit.normal.x);
				_collisionInfo.groundNormal = groundNormal;
				float groundAngle = Vector2.Angle(Vector2.up, groundHit.normal);
				if (groundAngle > _maxSlopeAngle) _collisionInfo.slidingDownSlope = true;
				_collisionInfo.collidingBelow = true;

				if (!_collisionInfo.slidingDownSlope)
				{
					refVelocity.y = Mathf.Clamp(refVelocity.y, -(groundHit.distance - _skinWidth), groundHit.distance - _skinWidth);
					_velocity.y = 0;
				}

				return;
			}
		}

		//TODO: add gravity to current slope normal y value

		// Check collision
		float differencePerRay = (_feetWidth - _skinWidth * 2) / (_verticalRayCount - 1);
		for (int i = 0; i < _verticalRayCount; i++)
        {
			Vector3 rayStartPos = startPosition - (transform.right * differencePerRay * i);

			float maxPossibleSlopeHeight = 0;
			float offsetFromCenter = Vector3.Distance(rayStartPos, transform.position + transform.up * _skinWidth);
			if (bottom)
			{
				maxPossibleSlopeHeight = Mathf.Abs(Mathf.Tan(_maxSlopeAngle * Mathf.Deg2Rad) * offsetFromCenter);
			}
			rayStartPos += transform.up * maxPossibleSlopeHeight;

			RaycastHit2D hit = Physics2D.Raycast(rayStartPos, bottom ? -transform.up : transform.up, rayLength + maxPossibleSlopeHeight * 2, _layerMask);

            if (hit) {
				if (hit.distance == 0) continue;
				
				// Check if center is grounded on a slope
				if (bottom && !Mathf.Approximately(0f, hit.normal.x))
				{
					//Vector2 groundNormal = new Vector2(hit.normal.y, -hit.normal.x) * (i > _verticalRayCount / 2f ? 1 : -1);
					//groundNormal *= Vector3.Distance(rayStartPos - transform.up * (maxPossibleSlopeHeight + _skinWidth), transform.position) / Mathf.Abs(groundNormal.x);
					//RaycastHit2D centerHit = Physics2D.Raycast((Vector3)hit.point + (Vector3)groundNormal + transform.up * _skinWidth, -transform.up, 2 * _skinWidth, _layerMask);
					RaycastHit2D centerHit = Physics2D.Raycast(transform.position + transform.up * _skinWidth, -transform.up, maxPossibleSlopeHeight + _skinWidth, _layerMask);
					if (centerHit)
					{
						//Debug.DrawRay((Vector3)hit.point + (Vector3)groundNormal + transform.up * _skinWidth, -transform.up * 2 * _skinWidth, Color.cyan);
						if(Vector2.Angle(centerHit.normal, Vector2.up) < _maxSlopeAngle) continue;
					}
				}

				// Check if side rays hit a slope less steep than the max angle
				if(bottom && Vector2.Angle(Vector2.up, hit.normal) < _maxSlopeAngle)
				{
					float currentSlopeHeight = Mathf.Abs(Mathf.Tan(Vector2.Angle(hit.normal, Vector2.up) * Mathf.Deg2Rad) * offsetFromCenter);
					bool upwards = (hit.normal.x > 0 && i < _verticalRayCount/2f) || (hit.normal.x < 0 && i > _verticalRayCount/2f);
					hit.distance = hit.distance - maxPossibleSlopeHeight - _skinWidth + currentSlopeHeight * (upwards ? -1 : 1);
				} else
				{
					hit.distance = hit.distance - maxPossibleSlopeHeight - _skinWidth;
				}
				if (bottom && hit.distance < maxPossibleSlopeHeight - 2 * _skinWidth && Vector2.Angle(Vector2.up, hit.normal) > _maxSlopeAngle) continue;
				if (hit.distance > Mathf.Abs(refVelocity.y)) continue;

				// Limit the velocity to the hitdistance
				refVelocity.y = bottom ? Mathf.Clamp(refVelocity.y, -(hit.distance), float.MaxValue) :
                                         Mathf.Clamp(refVelocity.y, float.MinValue, hit.distance);
				
				if (bottom)
				{
					Vector2 groundNormal = new Vector2(hit.normal.y, -hit.normal.x);
					_collisionInfo.groundNormal = groundNormal;
					float groundAngle = Vector2.Angle(Vector2.up, hit.normal);
					if (groundAngle > _maxSlopeAngle) _collisionInfo.slidingDownSlope = true;

					if (!_collisionInfo.slidingDownSlope)
					{
						refVelocity.y = Mathf.Clamp(refVelocity.y, -(hit.distance - _skinWidth), hit.distance - _skinWidth);
						_velocity.y = 0;
					}

					_collisionInfo.collidingBelow = true;
				}

				if (bottom) _collisionInfo.collidingBelow = true;
				if (!bottom) _collisionInfo.collidingUp = true;

				Debug.DrawLine(rayStartPos + (bottom ? -transform.up * maxPossibleSlopeHeight : Vector3.zero), rayStartPos + (bottom ? -transform.up : transform.up) * hit.distance, Color.red);
            } else {
                Debug.DrawLine(rayStartPos, rayStartPos + (bottom ? -transform.up : transform.up) * (rayLength + maxPossibleSlopeHeight * 2), Color.green);
            }
        }

		// Check if center is grounded
		if (bottom)
		{
			RaycastHit2D groundHit = Physics2D.Raycast(transform.position + transform.up * _skinWidth, -transform.up, rayLength, _layerMask);
			if (groundHit)
			{
				Vector2 groundNormal = new Vector2(groundHit.normal.y, -groundHit.normal.x);
				_collisionInfo.groundNormal = groundNormal;
				float groundAngle = Vector2.Angle(Vector2.up, groundHit.normal);
				if (groundAngle > _maxSlopeAngle) _collisionInfo.slidingDownSlope = true;

				if (!_collisionInfo.slidingDownSlope)
				{
					refVelocity.y = Mathf.Clamp(refVelocity.y, -(groundHit.distance - _skinWidth), groundHit.distance - _skinWidth);
					_velocity.y = 0;
				}

				_collisionInfo.collidingBelow = true;

				return;
			}
		}
	}

    // Debug Methods
    private void OnDrawGizmos() {
        // draw physics direction arrow
        Gizmos.color = new Color(1f, 0.4f, 0);
		// DrawArrow(transform.position + transform.up * transform.lossyScale.y * 0.5f, Physics2D.gravity);

		// draw feet and body width
		float colHeight = Application.isPlaying ? _colliderHeight : GetComponent<BoxCollider2D>().size.y;
		Gizmos.color = new Color(0.7f, 0.7f, 1f);
		Gizmos.DrawWireCube(transform.position + transform.up * transform.lossyScale.y * colHeight / 2, new Vector3(_bodyWidth, transform.lossyScale.y * colHeight, .1f));
		Gizmos.color = new Color(0.3f, 0.3f, 1f);
		Gizmos.DrawWireCube(transform.position + transform.up * transform.lossyScale.y * colHeight / 2, new Vector3(_feetWidth, transform.lossyScale.y * colHeight, .1f));

		void DrawArrow(Vector2 startPosition, Vector2 direction) {
            Vector2 arrowEnd = startPosition + direction / 20f;
            Vector2 arrowLength = startPosition + (direction.normalized * (direction.magnitude - 8f)) / 20f;
            Gizmos.DrawLine(startPosition, arrowEnd);
            Gizmos.DrawLine(arrowLength + new Vector2(direction.normalized.y, -direction.normalized.x) * -.3f, arrowEnd);
            Gizmos.DrawLine(arrowLength + new Vector2(-direction.normalized.y, direction.normalized.x) * -.3f, arrowEnd);
        }
    }

	// Data
	public struct CollisionInfo
	{
		public bool collidingLeft;
		public bool collidingRight;
		public bool collidingBelow;
		public bool collidingUp;

		public Vector2 groundNormal;
		public bool slidingDownSlope;

		public void Reset()
		{
			collidingLeft = collidingRight = collidingBelow = collidingUp = false;
			slidingDownSlope = false;
			groundNormal = new Vector2(0, 1);
		}
	}
}
