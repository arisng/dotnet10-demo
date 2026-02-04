namespace DProcess.Api.SkillCon.Common;
public static class DwGuids
{
    // Due Trainings related processing
    public static readonly Guid ProcessTypeRequiredTrainingsGuid = new Guid("455936B6-5E71-4496-88C8-334F3A2664A9");
    public static readonly string ProcessTypeRequiredTrainingsName = "RequiredTrainings";
    public static readonly Guid ProcessTypeDueTrainingsGuid = new Guid("54FD6750-0199-4be5-91D5-658C87F73CAB");
    public static readonly string ProcessTypeDueTrainingsName = "DueTrainings";
    public static readonly Guid ProcessTypeLatestSuccessfulForAllTrainingsGuid = new Guid("B83304C2-F937-492E-AB79-0513353F3016");
    public static readonly string ProcessTypeLatestSuccessfulForAllTrainingsName = "LatestSuccessfulForAllTrainings";
    public static readonly Guid ProcessTypeCleanupGuid = new Guid("7929FAC5-F3DB-4699-A69A-7FB39D4D715D");
    public static readonly string ProcessTypeCleanupName = "CleanupProcessedTasks";
    // Statistics related processing
    public static readonly Guid StatisticsTrainingComputationGuid = new Guid("17911EEA-852F-4EE3-882F-0DA2F3728761");
    public static readonly string StatisticsTrainingComputationName = "StatisticsTrainingComputation";
    public static readonly Guid StatisticsEmployeeComputationGuid = new Guid("F56B1CA9-E948-49C1-AB9C-7CB3272BA153");
    public static readonly string StatisticsEmployeeComputationName = "StatisticsEmployeeComputation";
}

public static class DailyTaskGuids
{
    public static readonly Guid ProcessTypeTemporalUpdatesGuid = new Guid("C68F609D-842F-4297-BAE6-C7034401E0CD");
    public static readonly string ProcessTypeTemporalUpdatesName = "TemporalUpdates";
    public static readonly Guid ProcessTypeHousekeepingGuid = new Guid("C8C724EC-A3E8-4155-BE49-D245D3604BD3");
    public static readonly string ProcessTypeHousekeepingName = "Housekeeping";
    public static readonly Guid ProcessTypeHousekeepingControlledDocsGuid = new Guid("EC95A62E-9A51-44F9-9769-A3EA825252E2");
    public static readonly string ProcessTypeHousekeepingControlledDocsName = "HousekeepingControlledDocs";
    public static readonly Guid ProcessTypeDeleteEmployeeUserNotLoggedInForOneMthGuid = new Guid("046170DC-BD19-4c12-A246-B85DE13BFBB8");
    public static readonly string ProcessTypeDeleteEmployeeUserNotLoggedInForOneMthName = "DeleteEmployeeUserNotLoggedInForOneMth";
    public static readonly Guid ProcessTypeDeleteEmployeeUserWithoutActiveEmploymentGuid = new Guid("FEDA95E7-5F36-442A-A376-BB930AC9F7B0");
    public static readonly string ProcessTypeDeleteEmployeeUserWithoutActiveEmploymentName = "DeleteEmployeeUserWithoutActiveEmployment";
    public static readonly Guid ComputeComplianceSimulationGuid = new Guid("3D9FB52D-169A-458a-AD7F-BD1F02DED725");
    public static readonly string ComputeComplianceSimulationName = "ComputeComplianceSimulation";
    public static readonly Guid UpdateSkillConAppointmentFromLMSGuid = new Guid("FE8A0CFE-4715-46D5-8E3F-87952437B68D");
    public static readonly string UpdateSkillConAppointmentFromLMSName = "UpdateSkillConAppointmentFromLMS";
    public static readonly Guid ProcessTypeUpdateAllAttendancesRegistersOfTrainingsGuid = new Guid("173F2DA5-6887-45AC-95FB-946A7CB9B299");
    public static readonly string ProcessTypeUpdateAllAttendancesRegistersOfTrainingsName = "UpdateAllAttendancesRegistersOfTrainings";
    public static readonly Guid ProcessTypeUpdateDailyElasticSearchIndexing = new Guid("DC2F57C0-7376-449E-B44B-37C4893D87A4");
    public static readonly string ProcessTypeUpdateDailyElasticSearchIndexingName = "UpdateDailyElasticSearchIndexing";
}

public static class GUIDsSetupForDefaultValues
{
    #region TraineeRoleGUID
    /// <summary>
    ///GUID For Default RoleTrainee (inherited from Class "RoleInOrganisation")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseRole]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('73a94985-e168-4743-92e4-958aa93af9ab',
    ///       'Trainee', 1, 'Trainee', 0, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid TraineeRoleGUID = new Guid("73a94985-e168-4743-92e4-958aa93af9ab");
    #endregion

    #region TrainerRoleGUID
    /// <summary>
    ///GUID For Default RoleTrainer (inherited from Class "RoleInOrganisation")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseRole]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('d2cf7509-09d9-4457-8a51-f36f36737306',
    ///       'Trainer', 2, 'Trainer', 0, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid TrainerRoleGUID = new Guid("d2cf7509-09d9-4457-8a51-f36f36737306");
    #endregion

    #region SupervisorRoleGUID
    /// <summary>
    ///GUID For Default RoleSupervisor (inherited from Class "RoleInOrganisation")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseRole]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('67943e87-ea76-444f-b23d-b2f543563b22',
    ///       'Supervisor', 3, 'Supervisor', 0, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid SupervisorRoleGUID = new Guid("67943e87-ea76-444f-b23d-b2f543563b22");
    #endregion

    #region HeadOfDepartmentRoleGUID
    /// <summary>
    ///GUID For Default RoleSupervisor (inherited from Class "RoleInOrganisation")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///IF NOT EXISTS
    ///   (SELECT * FROM BaseRole WHERE InternalId = '6AD9B0C8-B8D3-471d-988B-A0E4E4B047A8')
    ///        INSERT INTO BaseRole
    ///                   (InternalId
    ///                   ,RoleType
    ///                   ,Sequence
    ///                   ,Name
    ///                   ,Flag
    ///                   ,RowVersion
    ///                   ,Status)
    ///             VALUES
    ///                   ('6AD9B0C8-B8D3-471d-988B-A0E4E4B047A8',
    ///                    'HeadOfDepartment', 4, 'HeadOfDepartment', 0, 1, 0);
    ///
    /// </summary>
    ///
    public static readonly Guid HeadOfDepartmentRoleGUID = new Guid("6AD9B0C8-B8D3-471d-988B-A0E4E4B047A8");
    #endregion

    #region InstTypeTrainingOrgGUID
    /// <summary>
    ///GUID For Default InstitutionType (inherited from Class "BaseOrgType")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseOrgType]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('7016A750-71F4-48d8-B070-0F74ADEEFB97',
    ///       'InstitutionType', 1, 'Training Organisation', 0, 1, 0)
    ///
    /// </summary>
    ///
    public static readonly Guid InstTypeTrainingOrgGUID = new Guid("7016A750-71F4-48d8-B070-0F74ADEEFB97");
    #endregion

    #region OrgTypeDepartmentGUID
    /// <summary>
    ///GUID For Default OrganisationType (inherited from Class "BaseOrgType")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseOrgType]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('688fec2a-8a96-42a5-8566-6ebd83a629df',
    ///       'OrganisationType', 1, 'Department', 0, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid OrgTypeDepartmentGUID = new Guid("688fec2a-8a96-42a5-8566-6ebd83a629df");
    #endregion

    #region AppointmentStatusScheduledGUID
    /// <summary>
    ///GUID For Default AppointmentStatusType (inherited from Class "BaseCodeTable")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseCodeTable]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('32bd33fb-e1c9-4938-a0d5-bacfc2c582f8',
    ///       'AppointmentStatusType', 1, 'Scheduled', 0, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid AppointmentStatusScheduledGUID = new Guid("32bd33fb-e1c9-4938-a0d5-bacfc2c582f8");
    #endregion

    #region AppointmentStatusInvitedGUID
    /// <summary>
    ///GUID For Default AppointmentStatusType (inherited from Class "BaseCodeTable")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseCodeTable]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('22e0e556-d532-457e-a087-286414335b7d',
    ///       'AppointmentStatusType', 2, 'Invited', 0, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid AppointmentStatusInvitedGUID = new Guid("22e0e556-d532-457e-a087-286414335b7d");
    #endregion

    #region AppointmentStatusSuccessfulGUID
    /// <summary>
    ///GUID For Default AppointmentStatusType (inherited from Class "BaseCodeTable")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseCodeTable]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('d66ed546-aab7-4fb6-b5e5-e70addefa4c1',
    ///       'AppointmentStatusType', 3, 'Successful', 1, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid AppointmentStatusSuccessfulGUID = new Guid("d66ed546-aab7-4fb6-b5e5-e70addefa4c1");
    #endregion

    #region AppointmentStatusFailedGUID
    /// <summary>
    ///GUID For Default AppointmentStatusType (inherited from Class "BaseCodeTable")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseCodeTable]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('54b4c3c1-dd7c-4f86-8bdd-49ec8bbd14a1',
    ///       'AppointmentStatusType', 4, 'Failed', 0, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid AppointmentStatusFailedGUID = new Guid("54b4c3c1-dd7c-4f86-8bdd-49ec8bbd14a1");
    #endregion

    #region AppointmentStatusAbsentGUID
    /// <summary>
    ///GUID For Default AppointmentStatusType (inherited from Class "BaseCodeTable")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [SkillConR1].[dbo].[BaseCodeTable]
    ///      ([InternalId]
    ///      ,[RoleType]
    ///      ,[Sequence]
    ///      ,[Name]
    ///      ,[Flag]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('c096a42d-bc5d-4184-9298-e85dcb164283',
    ///       'AppointmentStatusType', 5, 'Absent', 0, 1,0)
    ///
    /// </summary>
    ///
    public static readonly Guid AppointmentStatusAbsentGUID = new Guid("c096a42d-bc5d-4184-9298-e85dcb164283");
    #endregion

    #region AttendanceType Elearning GUID
    /// <summary>
    /// GUID For Default AttendanceType (inherited from Class "BaseCodeTable")
    ///
    /// Appropriate SQL for Default values:
    ///
    /// DECLARE @Sequence INT;
    /// SELECT @Sequence = ISNULL(MAX(Sequence) + 1, 0) FROM BaseCodeTable WHERE CodeType='AttendanceType';
    /// INSERT INTO [dbo].[BaseCodeTable]
    ///       ([InternalId]
    ///       ,[CodeType]
    ///       ,[Sequence]
    ///       ,[Name]
    ///       ,[SystemUseOnly]
    ///       ,[Generic1] -- HideInTrainingCalender
    ///       ,[Generic2] -- IsSelfStudy
    ///       ,[RowVersion]
    ///       ,[Status])
    /// VALUES
    ///       ('2AE06A16-B2C6-461B-AB54-F6EC220BAFAC',
    ///        'AttendanceType',
    ///        @Sequence,
    ///        'E-Learning', 0, NULL, '1', 0, 0);
    /// </summary>
    public static readonly Guid AttendanceTyeELearningGUID = new Guid("2AE06A16-B2C6-461B-AB54-F6EC220BAFAC");
    #endregion

    #region AttendanceType Quiz GUID
    /// <summary>
    /// GUID For Default AttendanceType (inherited from Class "BaseCodeTable")
    ///
    /// Appropriate SQL for Default values:
    ///
    /// DECLARE @Sequence INT;
    /// SELECT @Sequence = ISNULL(MAX(Sequence) + 1, 0) FROM BaseCodeTable WHERE CodeType='AttendanceType';
    /// INSERT INTO [dbo].[BaseCodeTable]
    ///       ([InternalId]
    ///       ,[CodeType]
    ///       ,[Sequence]
    ///       ,[Name]
    ///       ,[SystemUseOnly]
    ///       ,[Generic1] -- HideInTrainingCalender
    ///       ,[Generic2] -- IsSelfStudy
    ///       ,[RowVersion]
    ///       ,[Status])
    /// VALUES
    ///       ('E8F97EE5-BDC4-4F82-9FB8-8E31FA59B9C2',
    ///        'AttendanceType',
    ///        @Sequence,
    ///        'Quiz', 0, NULL, '1', 0, 0);
    /// </summary>
    public static readonly Guid AttendanceTypeQuizGUID = new Guid("E8F97EE5-BDC4-4F82-9FB8-8E31FA59B9C2");
    #endregion

    #region SalutationMrGUID
    public static readonly Guid SalutationMrGUID = new Guid("3D21D673-78A3-40cf-87CE-88662EB0F577");
    #endregion

    #region SalutationMrsGUID
    public static readonly Guid SalutationMrsGUID = new Guid("E4EF3055-0F63-4f88-B8D6-461319467810");
    #endregion

    #region SalutationMsGUID
    public static readonly Guid SalutationMsGUID = new Guid("4A04B203-9FA7-489f-A966-6D20FC38605F");
    #endregion

    #region NotificationStatusQueuedGUID
    /// <summary>
    ///GUID For Default Queued (inherited from Class "BaseType")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [dbo].[BaseType]
    ///      ([InternalId]
    ///      ,[RowVersion]
    ///      ,[Status]
    ///      ,[Name]
    ///      ,[CodeType]
    ///      )
    ///VALUES
    ///      ('0FD7A75F-38CA-47ce-95C4-00BD8EBAAD76',
    ///       1, 0, 'Queued', 'NotificationStatusType')
    ///
    /// </summary>
    ///
    public static readonly Guid NotificationStatusQueuedGUID = new Guid("0FD7A75F-38CA-47ce-95C4-00BD8EBAAD76");
    #endregion

    #region NotificationStatusSentGUID
    /// <summary>
    ///GUID For Default Sent (inherited from Class "BaseType")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [dbo].[BaseType]
    ///      ([InternalId]
    ///      ,[RowVersion]
    ///      ,[Status]
    ///      ,[Name]
    ///      ,[CodeType]
    ///      )
    ///VALUES
    ///      ('28559702-65B9-4dd7-B31C-731A19A2584D',
    ///       1, 0, 'Sent', 'NotificationStatusType')
    ///
    /// </summary>
    ///
    public static readonly Guid NotificationStatusSentGUID = new Guid("28559702-65B9-4dd7-B31C-731A19A2584D");
    #endregion

    #region NotificationTypeInitialGUID
    /// <summary>
    ///GUID For Default Initial (inherited from Class "BaseType")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [dbo].[BaseType]
    ///      ([InternalId]
    ///      ,[RowVersion]
    ///      ,[Status]
    ///      ,[Name]
    ///      ,[CodeType]
    ///      )
    ///VALUES
    ///      ('4D6144FC-2135-4d37-BF8F-F5CE7AC3ECCD',
    ///       1, 0, 'Initial', 'NotificationType')
    ///
    /// </summary>
    ///
    public static readonly Guid NotificationTypeInitialGUID = new Guid("4D6144FC-2135-4d37-BF8F-F5CE7AC3ECCD");
    #endregion

    #region NotificationTypeReminderGUID
    /// <summary>
    ///GUID For Default Reminder (inherited from Class "BaseType")
    ///
    /// Appropriate SQL for Default values:
    ///
    ///INSERT INTO [dbo].[BaseType]
    ///      ([InternalId]
    ///      ,[RowVersion]
    ///      ,[Status]
    ///      ,[Name]
    ///      ,[CodeType]
    ///      )
    ///VALUES
    ///      ('6F24B91D-0D1B-46c6-8377-E9E9AE7F9B3A',
    ///       1, 0, 'Reminder', 'NotificationType')
    ///
    /// </summary>
    ///
    public static readonly Guid NotificationTypeReminderGUID = new Guid("6F24B91D-0D1B-46c6-8377-E9E9AE7F9B3A");
    #endregion

    #region TemplateTypePerAppointmentGUID
    /// <summary>
    ///GUID For Default TemplateType Per-Appointment
    ///
    /// Appropriate SQL for Default values:
    ///
    /// INSERT INTO [SkillConR1].[dbo].[TemplateType]
    ///      ([InternalId]
    ///      ,[Name]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('8ebcb130-68c1-4202-84a7-4dcfbfb1b44d',
    ///       'Per-Appointment', 1, 0)
    ///
    /// </summary>
    ///
    public static readonly Guid TemplateTypePerAppointmentGUID = new Guid("8ebcb130-68c1-4202-84a7-4dcfbfb1b44d");
    #endregion

    #region TemplateTypePerAttendantGUID
    /// <summary>
    ///GUID For Default TemplateType Per-Attendant
    ///
    /// Appropriate SQL for Default values:
    ///
    /// INSERT INTO [SkillConR1].[dbo].[TemplateType]
    ///      ([InternalId]
    ///      ,[Name]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('8235D290-BD8D-4fb8-9D8D-35A3B52ECC37',
    ///       'Per-Attendant', 1, 0)
    ///
    /// </summary>
    ///
    public static readonly Guid TemplateTypePerAttendantGUID = new Guid("8235D290-BD8D-4fb8-9D8D-35A3B52ECC37");
    #endregion

    #region TemplateTypeCertificateGUID
    /// <summary>
    ///GUID For Default TemplateType Certificate
    ///
    /// Appropriate SQL for Default values:
    ///
    /// INSERT INTO [SkillConR1].[dbo].[TemplateType]
    ///      ([InternalId]
    ///      ,[Name]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('F6CF65CB-8CD3-4cd8-BC00-2C146AB3C418',
    ///       'Certificate', 1, 0)
    ///
    /// </summary>
    ///
    public static readonly Guid TemplateTypeCertificateGUID = new Guid("F6CF65CB-8CD3-4cd8-BC00-2C146AB3C418");
    #endregion

    #region TemplateTypeAnnouncementGUID
    /// <summary>
    ///GUID For Default TemplateType Announcement
    ///
    /// Appropriate SQL for Default values:
    ///
    /// INSERT INTO [SkillConR1].[dbo].[TemplateType]
    ///      ([InternalId]
    ///      ,[Name]
    ///      ,[RowVersion]
    ///      ,[Status])
    ///VALUES
    ///      ('E624055E-1960-4c4b-B209-C04B70023259',
    ///       'Announcement', 1, 0)
    ///
    /// </summary>
    ///
    public static readonly Guid TemplateTypeAnnouncementGUID = new Guid("E624055E-1960-4c4b-B209-C04B70023259");
    #endregion

    #region TemplateTypeNotificationEscalationGUID

    /// <summary>
    /// <para>GUID For Default TemplateType for Notification Escalation.</para>
    /// <para>Appropriate SQL for Default values:</para>
    /// INSERT INTO [SkillConR1].[dbo].[TemplateType]
    ///      ([InternalId]
    ///      ,[Name]
    ///      ,[RowVersion]
    ///      ,[Status])
    /// VALUES
    ///      ('62E706E6-28B8-4B42-B9CD-44D574F635D4',
    ///       'Notification Escalation', 1, 0)
    ///
    /// </summary>
    ///
    public static readonly Guid TemplateTypeNotificationEscalationGUID = new Guid("62E706E6-28B8-4B42-B9CD-44D574F635D4");
    #endregion

    #region TemplateTypePortalRelatedGUID

    /// <summary>
    /// <para>GUID For Default TemplateType for Portal-related Template.</para>
    /// <para>Appropriate SQL for Default values:</para>
    /// INSERT INTO [SkillConR1].[dbo].[TemplateType]
    ///      ([InternalId]
    ///      ,[Name]
    ///      ,[RowVersion]
    ///      ,[Status])
    /// VALUES
    ///      ('E7F333D7-BB2D-4CEA-8161-661CF7BF0651',
    ///       'Portal-Related', 1, 0)
    ///
    /// </summary>
    ///
    public static readonly Guid TemplateTypePortalRelatedGUID = new Guid("E7F333D7-BB2D-4CEA-8161-661CF7BF0651");
    #endregion

    #region TemplateDefinitionPortalRegistrationGUID
    public static readonly Guid TemplateDefinitionPortalRegistrationGUID = new Guid("037BCEBB-EF1E-4FE2-8BDD-2C22D615C266");
    #endregion

    #region TemplateDefinitionPortalActivationGUID
    public static readonly Guid TemplateDefinitionPortalActivationGUID = new Guid("6F29159B-3710-4971-A431-25C2CD4A5D58");
    #endregion

    #region TemplateDefinitionPortalLostPasswordGUID
    public static readonly Guid TemplateDefinitionPortalLostPasswordGUID = new Guid("8BF15EA5-D2B0-4C92-8A6B-2D4BBAE354B3");
    #endregion

    #region Appointment Address, Email and Phone Types
    public static readonly Guid AppointmentAddressTypeGUID = new Guid("2136F024-BD57-4eb1-B12C-712C1A8D2274");
    public static readonly Guid AppointmentEmailTypeGUID = new Guid("BB22BDBA-2500-4388-96A7-F023A7564ADB");
    public static readonly Guid AppointmentPhoneTypeGUID = new Guid("4BE40525-218B-4e9c-9C79-9464B5E6EEDB");
    #endregion Appointment Address, Email and Phone Types

    #region Default Address, Email and Phone Types
    public static readonly Guid DefaultAddressTypeGUID = new Guid("AE7615D4-F68E-427b-AE45-10B245830163");
    public static readonly Guid DefaultEmailTypeGUID = new Guid("4F35DDE4-5DB3-4b8f-8710-8914F113889B");
    public static readonly Guid DefaultPhoneTypeGUID = new Guid("C1D12784-B51F-4d26-945F-96273875607A");
    #endregion Default Address, Email and Phone Types

    #region Root TrainingGroup
    public static readonly Guid RootTrainingGroupGUID = new Guid("5a23ee6f-adaf-48f6-b248-07ff21169bc4");
    public static readonly Guid MediaSpaceTrainingGroupGUID = new Guid("019ac48e-1e53-7ed1-ad97-e2f130c5c153");
    #endregion

    #region DueLeadTimeInDays
    public static readonly Guid DueLeadTimeInDaysGUID = new Guid("E001E0AE-C252-4c80-8911-F4B69710DFDF");
    #endregion

    #region CustomerId


    /// <summary>   Unique identifier for the customer. This is to identify the customer/his installation uniquely</summary>
    public static readonly Guid CustomerIdGUID = new Guid("7F65D6A4-5820-4FD5-84AF-1C2745B734AA");
    #endregion

    #region Cryptograhic Keys
    public static readonly Guid CryptoKeyGUID = new Guid("4d0b1cf9-8ffc-4fe9-ba19-022e33341ee1");
    public static readonly Guid CryptoInitVectorGUID = new Guid("67af4826-2872-4a56-89aa-213106e5fd3b");
    #endregion

    #region SMTPHost
    public static readonly Guid SMTPHostGUID = new Guid("BD02D145-0BDC-4397-80E7-5460775172B7");
    public static readonly Guid SMTPPortGUID = new Guid("0BD3663D-F08E-453b-8AD4-770F73207592");
    public static readonly Guid SMTPUserNameGUID = new Guid("F8580017-9FA0-4063-A5EF-37D75E93EFA5");
    public static readonly Guid SMTPPasswordGUID = new Guid("ED80DF8D-33CC-4a62-B6A9-2F5635D834E3");
    public static readonly Guid SMTPUseSSLGUID = new Guid("7C13FD6B-96E1-4a87-A317-ECD1BE473429");
    public static readonly Guid SMTPUseDefaultCredentialsGUID = new Guid("48F96526-5163-4B4D-8758-F18623E70E48");
    public static List<Guid> SMTPGuids
    {
        get
        {
            List<Guid> smtpGuids = new List<Guid>();
            smtpGuids.Add(SMTPHostGUID);
            smtpGuids.Add(SMTPPortGUID);
            smtpGuids.Add(SMTPUserNameGUID);
            smtpGuids.Add(SMTPPasswordGUID);
            smtpGuids.Add(SMTPUseSSLGUID);
            smtpGuids.Add(SMTPUseDefaultCredentialsGUID);
            return smtpGuids;
        }
    }
    #endregion SMTPHost

    #region Appearance
    public static readonly Guid AppearanceIntegratedRecsFontColor = new Guid("{FDDCC83B-E99E-4CEF-A4EA-D94579D8E874}");
    public static readonly Guid AppearanceIntegratedRecsBgColor = new Guid("{859333EB-A255-46D5-9425-C5AF3BBE5CB0}");

    public static List<Guid> AppearanceGuids
    {
        get
        {
            List<Guid> appearanceGuids = new List<Guid>();
            appearanceGuids.Add(AppearanceIntegratedRecsBgColor);
            appearanceGuids.Add(AppearanceIntegratedRecsFontColor);

            return appearanceGuids;
        }
    }
    #endregion SMTPHost

    #region MaxAttachedFileSizeInMB
    public static readonly Guid MaxAttachedFileSizeInMBGUID = new Guid("35CC58D5-0F6C-4a2f-8CA6-558B7F7ADFE0");
    #endregion MaxAttachedFileSizeInMB

    #region SimulatedSystemDate
    public static readonly Guid SimulatedSystemDateGUID = new Guid("575A8ED0-60A8-4186-9EAA-E724FC068FEC");
    #endregion SimulatedSystemDate

    #region FullDayApptStartTime
    public static readonly Guid FullDayApptStartTimeGUID = new Guid("7AF1A8C1-6F36-4C02-A0C3-26C77BF2E95A");
    #endregion FullDayApptStartTime

    #region FullDayApptEndTime
    public static readonly Guid FullDayApptEndTimeGUID = new Guid("67703AAD-8593-4BB4-88BD-7C36343C97F9");
    #endregion FullDayApptEndTime

    #region SysUser
    //public static readonly Guid SysUserGUID = DProcess.Core.Defines.LafiteGUID;
    public static readonly Guid SysUserGUID = new Guid("24F9EB48-FF51-4FC5-8ACF-C1B864B2781B");
    #endregion SysUser

    #region SimulatedUser
    public static readonly Guid SimulatedUserGUID = new Guid("9C98392E-1715-4f19-A684-3AD390E4DD2C");
    #endregion SimulatedUser

    #region HRSystem
    public static readonly Guid HRSystemGUID = new Guid("2c123115-65f8-4be3-abcd-ea3811fc76d2");
    #endregion HRSystem

    #region WSUser
    public static readonly Guid WSUserGUID = new Guid("5B1CE621-D78F-4C40-BB90-9F189F85DE05");
    #endregion

    #region dwProductionDBGuid
    public static readonly Guid dwProductionDBGuid = new Guid("9CD3CA58-E628-48A9-800D-082D5330402B");
    #endregion dsProductionDBGuid

    #region ActiveEmployeesCount
    public static readonly Guid ActiveEmployeesCountGuid = new Guid("FE709387-8B53-4127-B7DF-FBCDB03968EA");
    #endregion ActiveEmployeesCount

    #region Workflow Roles
    public static readonly Guid wfRoleEditorGuid = new Guid("67EA425D-6EB1-4EB1-B10A-CC43D78448BD");
    public static readonly Guid wfRoleOwnerGuid = new Guid("4788AAA5-BB37-4B9B-A35E-022C7FB7E9B6");
    public static readonly Guid wfRoleReviewerGuid = new Guid("424A6729-EF25-4167-85AD-DE903515032D");
    public static readonly Guid wfRoleApproverGuid = new Guid("11E4DACF-24E8-4526-BB55-EC58579B4A19");
    #endregion Workflow Roles

    #region OrganisationGuids
    public static readonly Guid OrganisationTransitGuid = new Guid("92ABC5F8-820E-40F3-850B-A453DDAEEB26");
    public static readonly Guid OrganisationObsoletedGuid = new Guid("77B6C4BC-FF67-4D2F-8C77-15A6B693632B");
    #endregion

    #region Requirement ID config
    public static readonly Guid ReqIdConfigGuid = new Guid("F3D2941F-C264-433F-892A-397363C4DF55");
    #endregion

    #region QuizResourceLink ID config
    public static readonly Guid QuizResourceLink = new Guid("74cb9cec-70c5-4cd4-abc3-42fd27b0bd92");
    #endregion

    #region Fee1 Title config
    public static readonly Guid Fee1Title = new Guid("BD509284-4D48-4352-A982-151845AEEFBA");
    #endregion

    #region Fee2 Title config
    public static readonly Guid Fee2Title = new Guid("C1075CC7-AA9D-4391-BFD8-218A35E9A3E0");
    #endregion

    #region Fee3 Title config
    public static readonly Guid Fee3Title = new Guid("2D531984-8CAC-4234-8417-9B9692752C2B");
    #endregion

    #region MediaSpace ResourceLinkType GUID
    public static readonly Guid MediaSpaceUrlResourceLinkTypeId = new Guid("BDFEF785-C931-4015-88DD-5271A2A40C5C");
    #endregion

    #region MediaSpace TrainingType GUID
    public static readonly Guid MediaSpaceTrainingTypeId = new Guid("808874D4-D561-4A30-88FE-FD5F52300261");
    #endregion
}