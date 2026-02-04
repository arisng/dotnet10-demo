namespace DProcess.Api.SkillCon.Common;

public static class SkillConConstants
{
    public static class ResourceLinkTypes
    {
        public const string MediaSpaceFileViewerUrlResourceLinkType = "MediaSpace URL";

        [Obsolete("Use MediaSpaceFileViewerUrlResourceLinkType instead.")]
        public const string MediaSpaceFileIdResourceLinkType = "Meta_MediaSpaceFileId";

        [Obsolete("Use MediaSpaceFileViewerUrlResourceLinkType instead.")]
        public const string MediaSpacesFileIdResourceLinkType = "SpaceFileId";
    }

    public static class TrainingTypes
    {
        public const string MediaSpace = "MediaSpace";

        [Obsolete("Use MediaSpaceTrainingType instead.")]
        public const string MediaSpaceTrainingType_Obsolete = "Spaces File";
    }
}
