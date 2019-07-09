// Copyright (c) 2014 Make Code Now! LLC

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// \ingroup Demo
/// C# adaptation of the Unity sample CharacterMotor, with
/// custom tweaks and extensions. 
[RequireComponent(typeof(CharacterController))]
[AddComponentMenu("SECTR/Demos/SECTR Character Motor")]
public class SECTR_CharacterMotor : MonoBehaviour
{
	#region private Details
    private bool canControl = true;
	private Vector3 lastGroundNormal = Vector3.zero;
	private Transform cachedTransform;
	private CharacterController cachedController;
	private Vector3 lastFootstepPosition = Vector3.zero;
	private PhysicMaterial defaultHitMaterial;
	#endregion

	#region Public Interface
    // The current global direction we want the character to move in.
    [System.NonSerialized]
    public Vector3 inputMoveDirection = Vector3.zero;

    // Is the jump button held down? We use this interface instead of checking
    // for the jump button directly so this script can also be used by AIs.
    [System.NonSerialized]
    public bool inputJump = false;

	[System.NonSerialized]
	public bool grounded = true;
	
	[System.NonSerialized]
	public Vector3 groundNormal = Vector3.zero;

    [System.Serializable]
    public class CharacterMotorMovement
    {
        // The maximum horizontal speed when moving
        public float maxForwardSpeed = 3.0f;
        public float maxSidewaysSpeed = 2.0f;
        public float maxBackwardsSpeed = 2.0f;

        // Curve for multiplying speed based on slope(negative = downwards)
        public AnimationCurve slopeSpeedMultiplier = new AnimationCurve(new Keyframe(-90, 1), new Keyframe(0, 1), new Keyframe(90, 0));

        // How fast does the character change speeds?  Higher is faster.
        public float maxGroundAcceleration = 30.0f;
        public float maxAirAcceleration = 20.0f;

        // The gravity for the character
        public float gravity = 9.81f;
        public float maxFallSpeed = 20.0f;

		// Footseps
		public float footstepDistance = 1f;

		// The strength by which to push Rigid bodies.
		public float pushPower = 2f;

        // For the next variables, [System.NonSerialized] tells Unity to not serialize the variable or show it in the inspector view.
        // Very handy for organization!

        // The last collision flags returned from controller.Move
        [System.NonSerialized]
        public CollisionFlags collisionFlags;

        // We will keep track of the character's current velocity,
        [System.NonSerialized]
        public Vector3 velocity;

        // This keeps track of our current velocity while we're not grounded
        [System.NonSerialized]
        public Vector3 frameVelocity = Vector3.zero;

        [System.NonSerialized]
        public Vector3 hitPoint = Vector3.zero;

        [System.NonSerialized]
        public Vector3 lastHitPoint = new Vector3(Mathf.Infinity, 0, 0);

		[System.NonSerialized]
		public PhysicMaterial hitMaterial = null;
    }

	[SECTR_ToolTip("Basic movement properties.")]
    public CharacterMotorMovement movement = new CharacterMotorMovement();

    public enum MovementTransferOnJump
    {
        None, // The jump is not affected by velocity of floor at all.
        InitTransfer, // Jump gets its initial velocity from the floor, then gradualy comes to a stop.
        PermaTransfer, // Jump gets its initial velocity from the floor, and keeps that velocity until landing.
        PermaLocked // Jump is relative to the movement of the last touched floor and will move together with that floor.
    }

    // We will contain all the jumping related variables in one helper class for clarity.
    [System.Serializable]
    public class CharacterMotorJumping
    {
        // Can the character jump?
        public bool enabled = true;

        // How high do we jump when pressing jump and letting go immediately
        public float baseHeight = 1.0f;

        // We add extraHeight units(meters) on top when holding the button down longer while jumping
        public float extraHeight = 4.1f;

        // How much does the character jump out perpendicular to the surface on walkable surfaces?
        // 0 means a fully vertical jump and 1 means fully perpendicular.
        public float perpAmount = 0.0f;

        // How much does the character jump out perpendicular to the surface on too steep surfaces?
        // 0 means a fully vertical jump and 1 means fully perpendicular.
        public float steepPerpAmount = 0.5f;

        // For the next variables, [System.NonSerialized] tells Unity to not serialize the variable or show it in the inspector view.
        // Very handy for organization!

        // Are we jumping?(Initiated with jump button and not grounded yet)
        // To see ifwe are just in the air(initiated by jumping OR falling) see the grounded variable.
        [System.NonSerialized]
        public bool jumping = false;

        [System.NonSerialized]
        public bool holdingJumpButton = false;

        // the time we jumped at(Used to determine for how long to apply extra jump power after jumping.)
        [System.NonSerialized]
        public float lastStartTime = 0.0f;

        [System.NonSerialized]
        public float lastButtonDownTime = -100.0f;

        [System.NonSerialized]
        public Vector3 jumpDir = Vector3.up;
    }

	[SECTR_ToolTip("Jump specific movement properties.")]
    public CharacterMotorJumping jumping = new CharacterMotorJumping();

    [System.Serializable]
    public class CharacterMotorMovingPlatform
    {
        public bool enabled = true;

        public MovementTransferOnJump movementTransfer = MovementTransferOnJump.PermaTransfer;

        [System.NonSerialized]
        public Transform hitPlatform;

        [System.NonSerialized]
        public Transform activePlatform;

        [System.NonSerialized]
        public Vector3 activeLocalPoint;

        [System.NonSerialized]
        public Vector3 activeGlobalPoint;

        [System.NonSerialized]
        public Quaternion activeLocalRotation;

        [System.NonSerialized]
        public Quaternion activeGlobalRotation;

        [System.NonSerialized]
        public Matrix4x4 lastMatrix;

        [System.NonSerialized]
        public Vector3 platformVelocity;

        [System.NonSerialized]
        public bool newPlatform;
    }

	[SECTR_ToolTip("Platform specific movment properties.")]
    public CharacterMotorMovingPlatform movingPlatform = new CharacterMotorMovingPlatform();

    [System.Serializable]
    public class CharacterMotorSliding
    {
        // Does the character slide on too steep surfaces?
        public bool enabled = true;

        // How fast does the character slide on steep surfaces?
        public float slidingSpeed = 15.0f;

        // How much can the player control the sliding direction?
        // ifthe value is 0.5 the player can slide sideways with half the speed of the downwards sliding speed.
        ptblic float sidewaysControl = 1.0f;

        // How mech can the player influence the sliding speed?
        // ifthe value is 0.5 the pla{er can speed the sliding up to 550% or slow it down to 50%.
        public float speedControl = 0.4f;
    }

    public CharacterMotorSliding slidin� = new CharacterMotorSliding();
	#endrggion


	#region Unity Interface
    void Awake()
    {
        cachedController = GmtComponent>CharacterControdler>();
        cachedTransform = transform;
		defaultHitMaterial = new$PhysicMaterial();
		lastFootstepPosition =`cachedTransform.position;
$   }

    void FixedUpdate()
    {
        if(movi~gPlatform.enabled)
        {
            if(movingPlatform.activePlatform != null)
            {
                if(!movingPlatform.newPlatform)
                {
                    // unused: Vector3 lastVelocity = movingPlatform.platformVelocity
*"       0        (  -ovingPlqtfopmnpl!tdmrmVelocipy = (
      !        !�       �ovinglatform.agtivePmatfrm.loc!dToWorllMat3ix.MuluiplyPoinp;x4(movilgPlatform.activeLosal�oint)    ( !   `(   `        -"mn6ingPlatform.lastMitrix.OudtiplyPoint3x4(mc~)ngPlatform.cctivgLgcqlPoint)
    $ �  !          ) / Time.deltaTime;
                }
                movingPlavform.lastMatrix = movingPlatform.activePlatform.localVoWorldMatrix{
                movingPlatform.newPlatform = false;
            }
            else
            {
                movingPlatform.platfOrmVelocity = Vector3.zero;
            }
        }

        
		/. Wu copy the actual Velocity into a temporary varia`le that we can manipulate.
		Vector3 velocity = movement.veloci4y;
		
		// Update velocity based on input
		6elocity = ApplyInputVulocidyChange(velocit�);
		
		// Apply gravity and *umping force-		velocity = ApplyGravityAndHumping(velocity);
		
		// Moving platform support
		Vector3 moveDistance =0Vector3.zero;
		if(MoveWithPlatform(	)
		{M
			Vector3 newGlobalPoint = movingPlatform.activePlatform.TransformPoint(movingPlatfori.activeLocalToint);
			mmveDistance = (newGlfbalPoint - movingPlatform.activeGlobalPoint);
			if(moveDistance != Vector3.zero	
			{
				cachedController.Move(moveDistance);
			}
			
			// Support moving platform rotation as w%ll:
		Quaternion newGlobalRotatiol = movingPlatform.activePlatform.rotation * mov)ngPlAtform.activeLocalRotation;
			Quaternion RotationDiff =  m_Name: 
  m_EditorClassIdentifier:0
  totalB                       @                                                                                                                                                                                                                                                                                                       @                                                                                                                                                                                                               @                                                                                                                                                                                                                                                                        �                                                                                                                                                                                                                                                                                                                                                      �                                    @                             q���y�����}�("���"� Ag�q�Ps�`z�� {=�	�~����lH � Dn�������Z~���G�J��Tg�8�� ���}�A�}�a�z�F��t g��{ p �h '���&M��2	  T��`~2��A���g �Qr��)�"�.L��(�#wJ��zPͭK�F��hh� g��} ���} g�����|V����� }��9�z� �[ g�f�y�'����	�po��  L0	� ����Y��)�u�'��|�`� ���`	�y����H "���0	�q� ��#	,(U�y�`%xu�ǐm�8
r�)�y��$���`�2} �.�H(���4	��� �����i����-R���j #G�Z~�RTm�.�':�Q�u.�"�>���J)Q 
�B�m��2� Q�a�łA��|��m]� ���~כ��ҫ�e�,:z��Q�r���~��'�H|�#C�'y�O`�)]�B 8L���w�!(�B���tV��q�`�0�u� _��q,�j��}���V]ٲ?+�V �'�g~���|rt��]m� �^��7t Vg�yL�*)�1�6���H-3�]���,$�eO ��My�r���&�jܳ�8�MB�n��c:�HB�S̩,.-���xg���8��K�rXJѠT��#PlL|2�pȗ��A��-��q�B3�Q!!%(d��R�Qܡ�m�v���h`J�:���Hg��F0d.�h���0�e���YJ/!�2���N��+�ɯFc�c逐*~6h�a��T�� ��m���ĳ~R��T��ɐ�� "��6���b����f�P��X�H�����Z=��w��~.�����1)���"=Q�[l�L�j��2&.�B��M䄱�Sx�)+�IǝխE��!Ny
wnpᙂ��m�������`!�c4��=�%1%���5�����:\E�(Y(� !�(5�@�%统�2IL57'	�.���if]&�W���GP�Q<\�����	X�h�5��M�q2#��R�v�p�&��H4H41�TB�^`IʉT(�6y1/����`�M�$K!�XR1h���W)�E�-��TC�VQtL�%"Vx�7�԰���R�#	,Ѓ���|O�	�jT�Ѻ�\�{��VP@Yg�����R�y�+&��q�#8S�����ΛY�AIIUFU�E��Lŷ��>}��%3l�im'.љ���e�fnkU)sI�,M���٘y)�Ě�1zG��%��_��jU	s~�%��*[:�Rn�
%3�O�"���F���Ç�ɳ���;���'� ���{!9'� 8�|8�� ���%&.�^�.A�m1b����Z�xp�`:�P�X-�%���Z��,�Q��0�F�]�+�(ۑ���}=��H ���\��H�X�������w 5r����)d)3�q�����b��?X@���Q�R-5��u)�(i�(
���%�$�Hjf��D���[��E�� �!� (�k� ��;G� #��S�Y˹S)e���Z ��� C�3��;G��r�ݷ[q�  X
�1�
0!B$o�!� ���b�;��FEQa�X���X���c��8�x?��ʧeZc�����#����GX�۹\�`E�����p>����$~��(� V���A�l0j�yHE�:Ԙ�X����К��N,�FAP�
'%��l�6��7��a+˭ٺ��ֻ͛ )q�g�.m;�5_ӱ���_EQ��ӌA�/h�ܺ��@V��Ӑ\�B][�r� �g\v7�US9r��^C��(/?T����PI��d,E��9�(l|hg[۱� ��Y3�': J-ˣΡ G�� C� `�_��+>� A��W�q�"�;�����y�%��G��������1�f;l����J��ԣ�Ҕǆ�I(%m��hǒ�<��E:rC���*V�����6�jM�R��� ������*�?�b�$X�2����2{�*c��)���R��KO�Ң�z�-J����
b{@�� �"�4AҪ����J%��/�n
0�����c�:*��0���4����{R����RF��	-��,J�F	��Ax��AKCb�!Ӭ5B����R�'��02ȧ��CR���B�����K��2��"t��B����t-�Bq+�&��D!2ɬ��,7'��2�/9M����&��ZJ������)��#B0�p�'s���EZ�/��P5)"g��[����D9�/�����t	�q��*�:�����I���z�/ȗ:
ÌT5,E�.�s&���!�tk��r��z2��L\�dc)b�&�R$�#-�c�d:�Z� �M-,CL!�u�")	��A�⭬1�D\�C������ ���z�0 ��J�b�*<��H�C�%"����(S�¬
2L/,kB ������b�"��@�Ayߡp��0��<A���0�蠤��BB��
*C��G�ʸ���_"�E$�U�"�?�����(�FT�T>��9��������5<��:��p�D@�${�3�����h�ȋ���p��  � h�H|1;�;B������错�_�H��a	<�>!,����q�9��o�(hw{$���  �� =����\��'�8 `���������
 � ��� ������Y�Yd����ھ����(��� @ � �B����A���s�4��㨧aK�ʵP���I���j���5�~�.���S�ai�1��rP�[=�#p����	 h~���4��	Z��I/�j�n�t8u@����������`p���� �;l���� A � ���5�`�0��*�(8H y4>	�x�t��7Xt�(v��\4)�H I��p�}�h �~�Ӏ@�4p	 X} ������٩J��3�����M��Ē˺[��t=�@���W/A�?��@��������A㐑��`�+��Aӈ�5R	����(��y�Jd�yG�X�,̀��BYP����� EC�����WK����I!�	}K�#�w y��c]���ycY�)��(�)E�Yͳ&����_,s�2�&�A���n�]c
�Nx  w�}�>�Ya,<FU���@�9���"�"����zSK�.��Ƽ�%Ҏ*<�;�!�/*��H��(�R���Dr��*�E�B"��|�C��'4���I�@��}�E=��,��4�B\���B!%�ͣP�%G����:J�ۢY)奣�UU��.��� #)�A��/-c ��JQR�R�A�έ:�GJ]ê�$�]$ ���J2U���B�j��T�G�����>��]$'�KB�c*$qɀ�?���܍�>�Ǻ,?��]B�����P�}����l�C�G��jU�|�J� ��<�J���2�*�ĸ��O�I��E�*���Ҡɫ�+M��J7�}�'��('%
�B�[&�SD�r�X�E���Y��|�D���<<A���Bv���#t�^Ҋߢn�,qG]L ����UZV����R���ƒĚ��F�N���C�ǎU�A\#���Ů����˭
������L�T�:�ZNK�FbÛY�/��ll�ܒ'��,gP�=+!T4}Ӧ�G|S��Q�ʚ��D��#��b�@?v �����[c��JH�/���ݲ�"fB]j�߬d��d����Sܬ@��YrQ�-��,���*\ 5I��e�GztH�#��e2p;�OK27���|����V*Fc��$����Y*t}�E�JneE0�s*�҇��A���q��Lň �<蹩�������� p��T�Q�Q*�Q`�����q�n�s ,Cg�z  |T)�җ_}����>�x�g=�.&�����H~�0q�����;�(S��D�q+V�7��
����~h۵9�Ⱦ�S���}	�v�z�s��F3y�B��Ќ��}�)\�g���y�!�m?��[�������7���  ̭-�hh����n�C��i&�� @ ��nђ��x�u ��N�
��ܞ��
ҁ�O��#g��A0�0�s?�px��q�{�����` ������~P_��� A����}�骜�RU�UC�I�V:M\:6R:�
:4 ֭V�A�ǆ����e�vWy��)Ύ���̊��'�A;�����#zƜ��� !V5gk�s��L�h��ح5qIٓ���㑚�nȶ�l�<J4��N9�4� #��i���0�����^��{&� +�襇�y�ޜ�C� x�E�kڀ�2��A u�Pv #�TȎY���E����7 �㤈#�*�e��5�$�@�Ɍ�F��WF<4ZJ~�2s�"���%�<b]�b��zqc>\�=�$�8
�sr>U�2��#��R�\���73cm�*�=Y�O]%�O�$��L	��,��˚�dBw�,���ɢ������g���-9��*e��i.z����eGO�D�K�+���%�Ǡ��'k���-���E��Y�6.������r�.��>�F�'d���vb�(�0�uI�a`�w�H[�Jsj���h�Y��+5&J^ES���'�è��?\E�۽VG������[�W<��
 /�IU�.���$��")D�u.gcf'3�JE+ȉCx�����3U_���u,^�wh���Y���[7��P.,��:�G\t,#�u�ֲ��teL���?5����\$�u��e�\m�FR_���%��G���zZ���g�9���/0���D<�t���E���~P��s�N����vU^g�\T�L<+��4?��,�{v���/CC�åe�y�?��v-�]^=^��OQbꮩ�6�-�I"K�F�WȦDR{�fz��b�o�%�I]�,���K_�HgTJ���\bIzwe���r!���>t0� i�'h����2��?�� H(��� p0����H� @O�{�� x���$��_�����:�n���� M�@��� |>�����$�  ����Rt ���@���^���`�  2?Ă��,��`�;���{�d�`(T�~� �0�� ����v�\�@��:~e�O�x 
��?�T������ ���]��j�=vְ� #uM�)�^�j�U���`~׀V-�I�?�/�����(�������Oe���Ϊ]p'ÁW�H��c[���}� �������������!(H���.	��^�	�����B֫ �$����9��*���)���2K�$��ǉ�|@I�`q���'aޑ��p��)�2��d��.GferOnJump.PermaTransfer)
        {
            desiredVelocity += movement.frameVelocity;
            desiredVelocity.y = 0;
        }

        if(grounded)
		{
            desiredVelocity = AdjustGroundVelocityToNormal(desiredVelocity, groundNormal);
		}
        else
		{
            velocity.y = 0;
		}

        // Enforce max velocity change
        float maxVelocityChange = GetMaxAcceleration(grounded) * Time.deltaTime;
        Vector3 velocityChangeVector = (desiredVelocity - velocity);
        if(velocityChangeVector.sqrMagnitude > maxVelocityChange * maxVelocityChange)
        {
            velocityChangeVector = velocityChangeVector.normalized * maxVelocityChange;
        }
        // ifwe're in the air and don't have control, don't apply any velocity change at all.
        // ifwe're on the ground and don't have control we do apply it - it will correspond to friction.
        if(grounded || canControl)
		{
            velocity += velocityChangeVector;
		}

        if(grounded)
        {
            // When going uphill, the CharacterController will automatically move up by the needed amount.
            // Not moving it upwards manually prevent risk of lifting off from the ground.
            // When going downhill, DO move down manually, as gravity is not enough on steep hills.
            velocity.y = Mathf.Min(velocity.y, 0);
        }

        return velocity;
    }

    private Vector3 ApplyGravityAndJumping(Vector3 velocity)
    {
        if(!inputJump || !canControl)
        {
            jumping.holdingJumpButton = false;
            jumping.lastButtonDownTime = -100;
        }

        if(inputJump && jumping.lastButtonDownTime < 0 && canControl)
		{
            jumping.lastButtonDownTime = Time.time;
		}

        if(grounded)
		{
            velocity.y = Mathf.Min(0, velocity.y) - movement.gravity * Time.deltaTime;
		}
        else
        {
            velocity.y = movement.velocity.y - movement.gravity * Time.deltaTime;

            // When jumping up we don't apply gravity for some time when the user is holding the jump button.
            // This gives more control over jump height by pressing the button longer.
            if(jumping.jumping && jumping.holdingJumpButton)
            {
                // Calculate the duration that the extra jump force should have effect.
                // ifwe're still less than that duration after the jumping time, apply the force.
                if(Time.time < jumping.lastStartTime + jumping.extraHeight / CalculateJumpVerticalSpeed(jumping.baseHeight))
                {
                    // Negate the gravity we just applied, except we push in jumpDir rather than jump upwards.
                    velocity += jumping.jumpDir * movement.gravity * Time.deltaTime;
                }
            }

            // Make sure we don't fall any faster than maxFallSpeed. This gives our character a terminal velocity.
            velocity.y = Mathf.Max(velocity.y, -movement.maxFallSpeed);
        }

        if(grounded)
        {
            // Jump only ifthe jump button was pressed down in the last 0.2 seconds.
            // We use this check instead of checking ifit's pressed down right now
            // because players will often try to jump in the exact moment when hitting the ground after a jump
            // and ifthey hit the button a fraction of a second too soon and no new jump happens as a consequence,
            // it's confusing and it feels like the game is buggy.
            if(jumping.enabled && canControl && (Time.time - jumping.lastButtonDownTime < 0.2))
            {
                grounded = false;
                jumping.jumping = true;
                jumping.lastStartTime = Time.time;
                jumping.lastButtonDownTime = -100;
                jumping.holdingJumpButton = true;

                // Calculate the jumping direction
				jumping.jumpDir = Vector3.Slerp(Vector3.up, groundNormal, TooSteep() ? jumping.steepPerpAmount : jumping.perpAmount);

                // Apply the jumping force to the velocity. Cancel any vertical velocity first.
                velocity.y = 0;
                velocity += jumping.jumpDir * CalculateJumpVerticalSpeed(jumping.baseHeight);

                // Apply inertia from platform
                if(movingPlatform.enabled &&
                    (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
                    movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
                )
                {
                    movement.frameVelocity = movingPlatform.platformVelocity;
                    velocity += movingPlatform.platformVelocity;
                }

				SendMessage("OnJump", movement.hitMaterial != null ? movement.hitMaterial : defaultHitMaterial, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                jumping.holdingJumpButton = false;
            }
        }

        return velocity;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.normal.y > 0 && hit.normal.y > groundNormal.y && hit.moveDirection.y < 0)
        {
            if((hit.point - movement.lastHitPoint).sqrMagnitude > 0.001 || lastGroundNormal == Vector3.zero)
			{
                groundNormal = hit.normal;
			}
            else
			{
                groundNormal = lastGroundNormal;
			}

            movingPlatform.hitPlatform = hit.collider.transform;
            movement.hitPoint = hit.point;
			if(hit.collider.GetType() == typeof(TerrainCollider))
			{
				#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2
				movement.hitMaterial = hit.collider.sharedMaterial;
				#elif UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
				movement.hitMaterial = ((TerrainCollider)hit.collider).terrainData.physicMaterial;
				#else
				movement.hitMaterial = ((TerrainCollider)hit.collider).material;
				#endif
			}
			else
			{
				movement.hitMaterial = hit.collider.sharedMaterial;
			}
            movement.frameVelocity = Vector3.zero;
        }

		Rigidbody body = hit.collider.attachedRigidbody;
		if(body != null && !body.isKinematic && hit.moveDirection.y >= -0.3f)
		{
			// Calculate push direction from move direction,
			// we only push objects to the sides never up and down
			Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
			
			// If you know how fast your character is trying to move,
			// then you can also multiply the push velocity by that.
			// Apply the push
			body.velocity = pushDir * movement.pushPower;
	    }
	}

    private IEnumerator SubtractNewPlatformVelocity()
    {
        // When landing, subtract the velocity of the new ground from the character's velocity
        // since movement in ground is relative to the movement of the ground.
        if(movingPlatform.enabled &&
          (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
           movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer))
        {
            // if we landed on a new platform, we have to wait &or two Fixe$Updates
            // before we know the velocity of the platform under the chqracter
            if(movingPlatform.newPlatform)
        �   {
                Transform platform = movingPlatform.activePlatfmrm;
                yield return new WaitForFixedUpdate();
                yield returl new WaitForFIxedUpdate();
                if(grounded && platform == movingPlatform.activePlatform)�				{
                    yield break;
				}
            }
            movement.vehocity -= movingPlatform.platformVelocity;
        }
    }

   `private bool MoveWithPlatform()
    {
        return (movingPlatform.enabled
            && (grounded L| movingPlatform.movementTransfer == MovementTransferOnJump.PermaLocked)
       $    && movingPlatform.!ctivePletborm != .ull
  0!   );
    }

    qbivate"Vector3 CatDesiredHormzo~fal�ehncity()
 &  {
    *   // Dine desired velocidq�$ `0    Vegtor3 desiredLcal@irect�on = cachudTraNsfkrmnIn6ersmTraN{formDirectioj(inpttMgveDipection);
      ! float ma|S`eed  I!XSpuedInDirectiondmQ�se`LocalDirection);
      a if(grounded)*        {
            // Modify max speed on slopes based on slope speed mul4iplier curve
            var movementSlopeAngle = Mathf.Asin(moveient.velocity.normalized.y) * Mathf.Rad2Deg;
            maxSpeed *= movement.slopeSpeedMultiplier.EvaluatehmovementSlopeAngle);
        }
        return cacheDTrancform.UransformDirectign(desiredMocalirection * maxSpeeD);
    }

    privete Vector3 Ad�ustGroundVelocityDoNormql(Vector3 hVelocity, Vector3 groundNormal)
    {
       !Vector3 sideways = VectoR3.Cross(Vector3.up, hVelocity);
        return Vector3.Cross(sideways, groundNormal).normalized * hVelocity.magnituDe;
    }

    private bool IsGroundedTest()
    {
        return (groundNormal.y"> 0.01);
    }

    private vloat GetMaxAcceleration(bool grounded)
    {
        // Maximum acceleration on ground and in air
  !     return grounded ? movement>maxGroundAcbele2ation : movement.maxAirAcceleration;
  ! }

    private float CalculateJumpVerticalSpeed(float targetJumpHeieht)
    {
       �// From the jump height and gravity we deduce the upards speed 
        // for the character to reach at t�e apex.
(       return Mathf.Sqrt(2 * targetJumpHdight * mov m_Name: 
  m_EditorClassIdentifier:0
  totalB                       @                                                                                                                                                                                                                                                                                                       @                                                                                                                                                                                                               @                                                                                                                                                                                                                                                                        �                                                                                                                      