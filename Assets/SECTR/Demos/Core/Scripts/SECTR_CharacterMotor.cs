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

    public CharacterMotorSliding slidinç = new CharacterMotorSliding();
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
      !        !        íovinglatform.agtivePmatfrm.loc!dToWorllMat3ix.MuluiplyPoinp;x4(movilgPlatform.activeLosalĞoint)    ( !   `(   `        -"mn6ingPlatform.lastMitrix.OudtiplyPoint3x4(mc~)ngPlatform.cctivgLgcqlPoint)
    $    !          ) / Time.deltaTime;
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
		6elocity = ApplyInputVulocidyChange(velocit¹);
		
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
  totalB                       @                                                                                                                                                                                                                                                                                                       @                                                                                                                                                                                                               @                                                                                                                                                                                                                                                                        €                                                                                                                                                                                                                                                                                                                                                      €                                    @                             qü‚ yú°çÑô}€("¬ŸÀ"Ø Agàq§Ps`z§¨ {=€	ü~Ÿ ó lH ë» DnÆğÀ¿‹œZ~¦’òGøJ§èTgè8Çè ‡ñî}‘Ağ}€aàz€FáÂt g‰æ{ p àh '£İø&M€ğ2	  T¨À`~2‰õA€‡Êg  Qr±²)…"˜.Lº¬(é#wJ©îzPÍ­KµF¶Åhh„ g™ğ} ‡è} g¹ó’à|V˜‡¨ }Ÿ©9äzŸ å[ g­f‡yä'‹å 	¾po–  L0	‚ ‰ş ÌYŸç)Ğu€'Ùò|©`¬ ©üô€`	æyàı¢H "Ÿ€Ø0	Ÿqè ÎÅ#	,(UçyÂ`%xuÇmœ8
r“)äy€$‚í¾„`ø2} à.—H(‡ğ4	§Ø €ÇÔŞŸi‘ùŸê-Rö•§j #GÒZ~¯RTmë.³':æ¿Q³u.È"í>´âë»J)Q 
µBÅm”µ2‚ Q˜aõÅ‚A‡è|ém]Ó  ²¨~×›°‡Ò«še¥,:z’„Q°r‡º«~âŸË'ôH|€#CŠ'y²O`±)]·B 8L¡íÂw½!(šB—©ªtVõq“`·0—u„ _§Õq,€jÎô}æÀíV]Ù²?+€V ã'g~Ùù®|rt½Â§]mŸ £^Ğê7t VgÈyLà*)î1ò6âöìH-3Ä]»â¹‚,$æeO æ¾öMyÅr­¬¯&ÄjÜ³½8í™MBˆnš™c:­HBƒSÌ©,.-°…˜xg ÙÕ8…™KãrXJÑ T†Â#PlL|2†pÈ—ˆ€A”Œ-¤qB3œQ!!%(d²–RäQÜ¡¦m¦v“ø”h`J:¼ÌHgŞÄF0d.ÃhÄ0e¢–óYJ/!‘2ÜN‰’+äÉ¯Fc£cé€*~6hña€†TÕ‹ áÓm‹…ªÄ³~R¢ùTŒí¨É¢» "¡œ6°œÅbêØÔÜfP¦XH‹“Ÿ”‘Z=‘‰wäÉ~.ªÇè•é1)ò½–"=Q™[l°Làjñò2&.âB‚ÒMä„±ŠSxä)+IÇÕ­E–Á!Ny
wnpá™‚¯‰m‘­¬ÎÍÉä`!”c4°Æ=Â˜Ñ%1%ñı°5¶½£Ä:\Eº(Y(” !¡(5‰@%ç»Ÿ’2IL57'	….²­µif]&¹Wœ¥öGPºQ<\Á½…Ò	Xùhé5±öM™q2#ËèRîv³p£&˜§H4H41ó¶TBà^`IÊ‰T(è6y1/Û—³`ÔM£$K!ÜXR1h•ºùW)ìE›-¢‹TC†VQtL­%"Vx7ÚÔ°Œõ¦RŠ#	,Ğƒôşš|O¬	®jT­Ñºş\µ{¨ÒVP@Yg£¼ù¯–RÇy¯+&¡´qé³#8S­©¥—¦Î›Y§AIIUFUæE‚LÅ·‡Ê>}µº%3l”im'.Ñ™‘£¹efnkU)sI¯,M®ÂÙ˜y)ÉÄš‡1zGÂù%§ë_´‡jU	s~ã%€¨*[:îRnå
%3¾Oš"ö£F‡ıÀÃ‡øÉ³€Áş;‡€ò'´ £§è{!9'Ã 8Ÿ|8Â ãùœ%&.¦^Š.Aˆm1bÅ×ÒÏZÃxp`:‡PíX-ë%¡÷“Z›È,“Q †0¨FŒ]ß+È(Û‘¡Øà}=àH Èı \ĞÀHşX£äŸæšÀƒ¤w 5rüÀŠ‚)d)3¢qî«ÑÜëØb¼Ú?X@€”Q¡R-5©u)§(iÇ(
¢Ùš%‚$ÜHjfŠŞD˜–ô[ĞúE­á €!êŒ (îkÅ Î;GŠ #ÀáS²YË¹S)eòÎÇZ ‡šÅ C¹3€Ú;G„r€İ·[qş  X
1ş
0!B$o!Ì ‡»ÔbÀ;³‡FEQaÀX°ÖÃX¸ıÆcğÿ8õx?ÖûÊ§eZc¦ùÀÀæ#¼­‘ÔGXìÛ¹\€`Eöæãğ‚p>ßİÀ€$~ (Ğ V¯¡ôA”l0jšyHE½:Ô˜ƒX£¦³ĞšÉÑN,°FAPê
'%ı—l6ƒô‡7Ñåa+Ë­Ùº¨ĞÍ›Ö» )qgğ.m; 5_Ó±‰ïÔ_EQ»æ ÓŒA‚/hÀÜºûÚ@VÃíÓ\¸B][¼rí ›g\v7’US9rúš^Cƒ¼(/?Tš–ôçPIİÉd,Eâ¬ø9ô(l|hg[Û±¸ ÃëY3Ÿ': J-Ë£Î¡ G£Ô Cí `ƒ_“Œ+>ù Aâ«ÔW«q"š;—ÍáÁÆyò%ÚËGˆû°ö¶ÂïµÚ1÷f;l¤×É‘J¯üÔ£ÒÒ”Ç†’I(%m‡¤hÇ’Ó<ŒŠE:rCŠ›Œ*V¡šÀ¦’6ªjM£R˜£Ä  †ˆ°­*ƒ?³bº$Xé§2¨ªû©2{¥*c¤±)Ò÷°R™¦KO Ò¢¦zœ-J÷¿àá
b{@ö© Š"4AÒª úäÁJ%§Ü/šn
0š¥ù°ªcö:*¦¡0¶»ª4ªÊÆÀ{R¯²ùÁRF§Â	-¥À,J¡F	›Axà›AKCb®!Ó¬5Bš­˜ã®RÌ'À­02È§œ›CR‰öÀBƒ²óö¿Kª¤2¯¦"t¡à½B¬«œ¿t-ÄBq+Š&¢äD!2É¬°,7'û¢2Ü/9M¬œÃã&Ÿ«ZJ”úµ©»Ğ)¢ƒ#B0ˆpÜ's¹‹üEZË/¸†P5)"gºº[¡û«¤D9œ/§ùÜ›ˆt	ºq²£*Ã:µº²¿òIÂ˜¥zº/È—:
ÃŒT5,EŒ.¨s&ª¯Ú!¨tk£ür§šz2şÆL\¤dc)bÔ&üR$”#-¢c‹d:©ZŸ ‰M-,CL!ªuÁ")	ªÂAˆâ­¬1­D\¥CÃôª’£© á¬ÌÂz¿0 »J‘bİ*<ÅÜHÅC¢%"üÇÈç(SıÂ¬
2L/,kB ÆÃÃüôbÆ"ŒÇ@ÏAyß¡p¬0æÉ<AÍÀª0¼è ¤ÃïBBÍÅ
*CŠïGìÊ¸«œ²_"ôE$äU«"í?ğÊË•Ã(šFT©T>­ò9º”³©œÃÒ5<ŒÉ:¯ÆpÇD@ä•${Ì3ÊÛÈ»hìÈ‹˜ ˜pğ¸  û h‘H|1;è‰;BÌğ²è‡ù“é”™Ñ_éHùa	<Ù>!,‹›ª€q‡9†ğo‡(hw{$ŸœÙ  Îë =³¶‡É\Ë'ù8 `óù‹È•ğ€°
  €±ƒ °ó‡ùŸ±YYdóÇñ–Ú¾áİàõ(‡èó @ ğ ‘B€¸‡ÙA€ûs4İ”ã¨§aKËÊµP’ºµI¥Ì j‰ Ñ5¨~™.À‚”SÑai“1ÂørP—[=¶#p€±	 h~ ë4…»	Z „I/€j¸n€t8u@€€À‡ğû“`pÍĞƒ  ë;lü‡ù€ A “ ¼‰¡5³`’0Îë*€(8H y4>	Áx€tµğ7Xt²(v‡\4)ŒH I€p}h Ø~áÓ€@ì4p	 X} —ˆˆÓËóŸ¹Ù©J­Õ3šµšÌMÃíÄ’Ëº[£ˆt=°@¢¬ıW/A²?›ï@ûªºèÄÕùèAã‘‘`ˆ+ÈùAÓˆÿ5R	Ê©¸Õ(yàJd¯yGœX™,Í€•BYPğ˜¸›È ECĞìª¥WK›ĞûäI!€	}K³#¼w y¤§c]ŸãàycYå´)ùØ(´)EØYÍ³&’ÑÇé_,sÂ2ã&A’‡µn]c
íNx  w‡}‘> Ya,<FUäà£@Î9ëÊô"Ê"Æüœ­zSKâ.¨”Æ¼¸%Ò*<¢;¾!»/*ÜÂHˆİ(ÅR¥º®Dr†Ú*ãEªB"¹º|¤Cª'4¯¬¼I¤@Ê}«E=œÁ,—ª4šB\¾²ùB!%ÚÍ£PĞ%G¥õ­«:JšÛ¢Y)å¥£òUUªò.šš” #)®AñË/-c ²ÂJQR¦RûAÈÎ­:¶GJ]ÃªÖ$ô]$ ¸­â†J2U¬€³B£jœİT¾G²ØÀô•>³û]$'ñKBŠc*$qÉ€¼?ªÂéÜÃ>íÇº,?ô§]Bªª³üPÆ} …ÒËl¹CâG¢ıjUœ|ËJË ìÄ<äJ›‰Ü2ê´*ó¤§Ä¸¯ÄOI¤ÌE·*¸­ÚÒ É«Ğ+M·ÕJ7¤} '­¿('%
¾B[&ƒSD´ršXœEú’²YîÈ|¹D‚Û<<A…®‰BvÁÍÇ#t¤^ÒŠß¢nÜ,qG]L ¯†ÄåUZVºµ¹İR˜¬¶Æ’ÄšíêFÒNÀ¼‡CğÇU®A\#¬”áÅ®¯³õ®Ë­
ŠœÙê¯ÜLÃT¾:ßZNKşFbÃ›YØ/ºŞll¡Ü’'â“ã,gPü=+!T4}Ó¦ÚG|S©‚Q­Êš’ıDô™#ŒªbŒ@?v ’¤«­[c•èJH/ò¿¯êôİ²¢"fB]j×ß¬dÊëd€®âüSÜ¬@…©YrQ°-ü²,˜’*\ 5Iª‰eØGztHÚ#ãËe2p;ŞOK27£’Æ|¾´¹ªV*Fc¢¾$¬Š‰äY*t}«EÙJneE0˜s*äÒ‡ùòA’ÑqÁ”LÅˆ ’<è¹©è’Ôä¸î€À€¡ pü€TÓQ¨Q*Q`–èÛä×qënøs ,Cg z  |T)òÒ—_}•äçÎ>·x÷g=ª.&¸òÌØ’H~0q…‹‚‹;¾(SëDÈîŒ˜q+V7ó
‡è™à~hÛµ9‘È¾åS«œâ}	êv­zˆsùºF3y¤B˜ĞŒºñ})\gÀÍØy€!‡m?†Ø[è·€Ìó‡ë7å™É’  Ì­-˜hh†È†àn‡C´Ìi&ğı @ †ĞnÑ’–°x•u €¸Nğ
€‘ÜÙÇ
ÒO“É#gÈîšA0ñ0¸s?ÎpxèøqÑ{şÌ¡¾` ø€À~P_°™í A¡´±û}ˆéªœ•RUUC¡I…V:M\:6R:¥
:4 Ö­V¢A²Ç†ó±·ˆeÖvWyíí)ÎñÚ‘ÌŠÁµ'ÂA;©³­áö#zÆœáçœ¤ !V5gkõs¡Lßhƒ‡Ø­5qIÙ“¸¼Üã‘šõnÈ¶lÜ<J4ş¬N9ä4« #À€i¸¶±0½‹å°ö^—Ÿ{&í +è¥‡ùy‰Şœ‘C² x–E²kÚ€î2ÁöA u‡Pv #íº«TÈY«ì©ËE£¯ˆÅ7 Àã¤ˆ#Ö*Ôe¯×5Ü$‰@ÉŒF“ıWF<4ZJ~Å2sº"éİô%ä<b]¬b¬Ùzqc>\¤=Ï$Ê8
úsr>U‚2©”#‚ÕRÖ\¥ßÌ73cmË*½=Y±O]%ÀOõ$µÊL	¥¾,’»Ëš›dBwÃ,ŒÂÅÉ¢íÚûşÊögˆæ-9´§*e Ìi.z±»ª¤eGO•D©Kõ+°™Å% Ç ŒÀ'kÊÂ¿-×İÆEÅÃYÌ6.¦÷§ƒ¡árÛ.¸â>¦FŞ'd­Ôâvb®(ö0İuI¯a`×wâH[÷Jsj‚óÎh£YÂÆ+5&J^ESãÕâ'ÜÃ¨Øô?\EœÛ½VG¤ßÚûÇô[¦W<£ü
 /ŒIUÊ.šÌ$ÀÒ")Dêu.gcf'3ŞJE+È‰Cx®ÍİÀï3U_œââ§u,^ÂwhàŒÌYßú¶[7£÷P.,Æ:ÅG\t,#ÉuÖÖ²¨ßteL¬¯«?5ôõÎ€\$uààe¤\mŞFR_üÃÊ%ÆõG¦¨¤zZƒôäg‚9ÕØû/0ø÷¯D<tÒæáE ª÷~PısÑN¯‡¶£vU^g‡\T±L<+Œş4?•Ö,„{vÜòá/CCºÃ¥e˜yâ?‡±v-®]^=^µãOQbê®©®6Ş-ÅI"KçFáWÈ¦DR{Êfzâøb¡o¢%ŞI]‰,”¿K_æHgTJÔÉÖ\bIzwe¦ˆr!ç÷>t0˜ i·'hƒÕÍ¬2€€?ßÀ H(ÿÁ  p0ş„ğ¨Hş @O÷{Íì x¼€$€_È‰Èé:nàáòù Mß@Ûíø |>àëöŒ€$€  ¦€ªğRt ªÀé@«ş©^¬¿ `›  2?Ä‚ëğ,¿Á`;ıæõ{Éd€`(Tü~€ €0úß €£Ùñv¼\@‡è”:~e‚OÀx 
û¥?ßTÀş±ÓÖ ª®«]¤¥j¶=vÖ°ª #uMİ)ı^®j£UÎº½`~×€V-×I£?À/‡ÓôùÀ(¯ğµäö¸İOe¼æ¹Îª]p'ÃWìHı…c[Äíò}€ ÀçÉö†ùÌš¦Á¸ç¶!(HŸÀÈ.	Ÿç^	›¦ €’BÖ« $Ÿà£Şİ9çà*¸«ë´)çğº2Kª$ƒµÇ‰è|@Iè`qş°'aŞ‘¬p»¯)ì2çødçè.GferOnJump.PermaTransfer)
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
            {
                Transform platform = movingPlatform.activePlatfmrm;
                yield return new WaitForFixedUpdate();
                yield returl new WaitForFIxedUpdate();
                if(grounded && platform == movingPlatform.activePlatform)Š				{
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

    qbivate"Vector3 CatDesiredHormzo~falÖehncity()
 &  {
    *   // Dine desired velocidqŠ$ `0    Vegtor3 desiredLcal@irectéon = cachudTraNsfkrmnIn6ersmTraN{formDirectioj(inpttMgveDipection);
      ! float ma|S`eed  I!XSpuedInDirectiondmQése`LocalDirection);
      a if(grounded)*        {
            // Modify max speed on slopes based on slope speed mul4iplier curve
            var movementSlopeAngle = Mathf.Asin(moveient.velocity.normalized.y) * Mathf.Rad2Deg;
            maxSpeed *= movement.slopeSpeedMultiplier.EvaluatehmovementSlopeAngle);
        }
        return cacheDTrancform.UransformDirectign(desiredMocalirection * maxSpeeD);
    }

    privete Vector3 AdèustGroundVelocityDoNormql(Vector3 hVelocity, Vector3 groundNormal)
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
        // From the jump height and gravity we deduce the upards speed 
        // for the character to reach at tèe apex.
(       return Mathf.Sqrt(2 * targetJumpHdight * mov m_Name: 
  m_EditorClassIdentifier:0
  totalB                       @                                                                                                                                                                                                                                                                                                       @                                                                                                                                                                                                               @                                                                                                                                                                                                                                                                        €                                                                                                                      