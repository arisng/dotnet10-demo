using System.Runtime.Serialization;

namespace DProcess.Api.SkillCon.Common;
public static class SkillConEnums
{
    [Flags]
    public enum ConfirmationType
    {
        None = 1,
        SelfStudy = 2,
        Quiz = 4,
    }

    /// <summary>
    /// This is used for soft deletes to indicate if entity is deleted.
    /// Enums other than Active and Deleted are for future.
    /// </summary>
    public enum EntityStatus
    {
        Active = 0,
        Deleted = 1,
        Archived = 2,
        Deleting = 3, // before delete is committed
        Expired = 10,
        Undefined = 99
    }

    /// <summary>
    /// Instance state types for training versions (from legacy DProcess code).
    /// </summary>
    public enum InstanceStateTypes
    {
        Draft = 10,
        Approved = 30,
        Obsolete = 50,
        Undefined = 99
    }

    public enum SpQueryEntityType
    {
        Organisation = 1,
        Supervisor = 2,
        Employee = 3,
        TrainingGroup = 4,
        Training = 5,
        HeadPerson = 6
    }

    public enum QuestionTypeEnum
    {
        [EnumMember(Value ="QuestionTypeEnumDescription_SingleChoice")]
        SingleChoice = 10,
        [EnumMember(Value= "QuestionTypeEnumDescription_MultipleChoice")]
        MultipleChoice = 20
    }
}
