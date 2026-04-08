namespace AvatarSystem
{
    /// <summary>
    /// Equipment slot types for the modular outfit system.
    /// </summary>
    public enum SlotType
    {
        Hair,
        HairAccessory,
        Top,
        Bottom,
        Dress,
        Socks,
        Shoes,
        Gloves,
        BraceletL,
        BraceletR,
        // Reserved for future expansion
        BodyVariant,
        FaceVariant,
        AccessoryExtra
    }

    /// <summary>
    /// Body regions that can be hidden when outfit items are equipped.
    /// </summary>
    public enum BodyRegion
    {
        Head,
        TorsoUpper,
        TorsoLower,
        ArmUpperL,
        ArmUpperR,
        ForearmL,
        ForearmR,
        HandL,
        HandR,
        ThighL,
        ThighR,
        CalfL,
        CalfR,
        FootL,
        FootR
    }

    /// <summary>
    /// Viseme types for lip-sync blendshape mapping.
    /// </summary>
    public enum VisemeType
    {
        Rest,
        AA,
        E,
        I,
        O,
        U,
        FV,
        L,
        MBP
    }

    /// <summary>
    /// High-level conversation states that drive avatar behavior.
    /// </summary>
    public enum ConversationState
    {
        Dormant,
        Idle,
        Listening,
        Thinking,
        Speaking,
        Reacting,
        Waiting
    }

    /// <summary>
    /// Emotion types for facial expression blending.
    /// </summary>
    public enum EmotionType
    {
        Neutral,
        SoftSmile,
        Happy,
        Excited,
        Sad,
        Concerned,
        Surprised,
        Embarrassed,
        Curious,
        Focused,
        Sleepy,
        Apologetic
    }

    /// <summary>
    /// Body gesture types used by the gesture animation layer.
    /// </summary>
    public enum GestureType
    {
        None,
        Explain,
        Nod,
        HeadShake,
        Wave,
        SmallEmphasis,
        ThinkingPose,
        Greeting
    }

    /// <summary>
    /// How an accessory item attaches to the avatar.
    /// </summary>
    public enum AnchorType
    {
        None,
        BoneAttach,
        SocketAttach
    }
}
