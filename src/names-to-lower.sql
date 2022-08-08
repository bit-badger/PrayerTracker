-- Church
ALTER TABLE pt."Church" RENAME COLUMN "ChurchId" TO id;
ALTER TABLE pt."Church" RENAME COLUMN "Name" TO church_name;
ALTER TABLE pt."Church" RENAME COLUMN "City" TO city;
ALTER TABLE pt."Church" RENAME COLUMN "ST" TO state;
ALTER TABLE pt."Church" RENAME COLUMN "HasVirtualPrayerRoomInterface" TO has_vps_interface;
ALTER TABLE pt."Church" RENAME COLUMN "InterfaceAddress" TO interface_address;
ALTER TABLE pt."Church" RENAME CONSTRAINT "PK_Church" TO pk_church;
ALTER TABLE pt."Church" RENAME TO church;

-- List Preference
ALTER TABLE pt."ListPreference" RENAME COLUMN "SmallGroupId" TO small_group_id;
ALTER TABLE pt."ListPreference" RENAME COLUMN "DaysToExpire" TO days_to_expire;
ALTER TABLE pt."ListPreference" RENAME COLUMN "DaysToKeepNew" TO days_to_keep_new;
ALTER TABLE pt."ListPreference" RENAME COLUMN "LongTermUpdateWeeks" TO long_term_update_weeks;
ALTER TABLE pt."ListPreference" RENAME COLUMN "EmailFromName" TO email_from_name;
ALTER TABLE pt."ListPreference" RENAME COLUMN "EmailFromAddress" TO email_from_address;
ALTER TABLE pt."ListPreference" RENAME COLUMN "ListFonts" TO fonts;
ALTER TABLE pt."ListPreference" RENAME COLUMN "HeadingColor" TO heading_color;
ALTER TABLE pt."ListPreference" RENAME COLUMN "LineColor" TO line_color;
ALTER TABLE pt."ListPreference" RENAME COLUMN "HeadingFontSize" TO heading_font_size;
ALTER TABLE pt."ListPreference" RENAME COLUMN "TextFontSize" TO text_font_size;
ALTER TABLE pt."ListPreference" RENAME COLUMN "RequestSort" TO request_sort;
ALTER TABLE pt."ListPreference" RENAME COLUMN "GroupPassword" TO group_password;
ALTER TABLE pt."ListPreference" RENAME COLUMN "DefaultEmailType" TO default_email_type;
ALTER TABLE pt."ListPreference" RENAME COLUMN "IsPublic" TO is_public;
ALTER TABLE pt."ListPreference" RENAME COLUMN "TimeZoneId" TO time_zone_id;
ALTER TABLE pt."ListPreference" RENAME COLUMN "PageSize" TO page_size;
ALTER TABLE pt."ListPreference" RENAME COLUMN "AsOfDateDisplay" TO as_of_date_display;
ALTER TABLE pt."ListPreference" RENAME CONSTRAINT "PK_ListPreference" TO pk_list_preference;
ALTER TABLE pt."ListPreference" RENAME CONSTRAINT "FK_ListPreference_SmallGroup_SmallGroupId" TO fk_list_preference_small_group_id;
ALTER TABLE pt."ListPreference" RENAME CONSTRAINT "FK_ListPreference_TimeZone_TimeZoneId" TO fk_list_preference_time_zone_id;
ALTER TABLE pt."ListPreference" RENAME TO list_preference;

ALTER INDEX pt."IX_ListPreference_TimeZoneId" RENAME TO ix_list_preference_time_zone_id;

-- Small Group Member
ALTER TABLE pt."Member" RENAME COLUMN "MemberId" TO id;
ALTER TABLE pt."Member" RENAME COLUMN "SmallGroupId" TO small_group_id;
ALTER TABLE pt."Member" RENAME COLUMN "MemberName" TO member_name;
ALTER TABLE pt."Member" RENAME COLUMN "Email" TO email;
ALTER TABLE pt."Member" RENAME COLUMN "Format" TO email_format;
ALTER TABLE pt."Member" RENAME CONSTRAINT "PK_Member" TO pk_member;
ALTER TABLE pt."Member" RENAME CONSTRAINT "FK_Member_SmallGroup_SmallGroupId" TO fk_member_small_group_id;
ALTER TABLE pt."Member" RENAME TO member;

ALTER INDEX pt."IX_Member_SmallGroupId" RENAME TO ix_member_small_group_id;

-- Prayer Request
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "PrayerRequestId" TO id;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "RequestType" TO request_type;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "UserId" TO user_id;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "SmallGroupId" TO small_group_id;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "EnteredDate" TO entered_date;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "UpdatedDate" TO updated_date;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "Requestor" TO requestor;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "Text" TO request_text;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "NotifyChaplain" TO notify_chaplain;
ALTER TABLE pt."PrayerRequest" RENAME COLUMN "Expiration" TO expiration;
ALTER TABLE pt."PrayerRequest" RENAME CONSTRAINT "PK_PrayerRequest" TO pk_prayer_request;
ALTER TABLE pt."PrayerRequest" RENAME CONSTRAINT "FK_PrayerRequest_User_UserId" TO fk_prayer_request_user_id;
ALTER TABLE pt."PrayerRequest" RENAME CONSTRAINT "FK_PrayerRequest_SmallGroup_SmallGroupId" TO fk_prayer_request_small_group_id;
ALTER TABLE pt."PrayerRequest" RENAME TO prayer_request;

ALTER INDEX pt."IX_PrayerRequest_UserId" RENAME TO ix_prayer_request_user_id;
ALTER INDEX pt."IX_PrayerRequest_SmallGroupId" RENAME TO ix_prayer_request_small_group_id;
ALTER INDEX pt."IX_PrayerRequest_Requestor_TRGM" RENAME TO ix_prayer_request_trgm_requestor;
ALTER INDEX pt."IX_PrayerRequest_Text_TRGM" RENAME TO ix_prayer_request_trgm_request_text;
-- Small Group
ALTER TABLE pt."SmallGroup" RENAME COLUMN "SmallGroupId" TO id;
ALTER TABLE pt."SmallGroup" RENAME COLUMN "ChurchId" TO church_id;
ALTER TABLE pt."SmallGroup" RENAME COLUMN "Name" TO group_name;
ALTER TABLE pt."SmallGroup" RENAME CONSTRAINT "PK_SmallGroup" TO pk_small_group;
ALTER TABLE pt."SmallGroup" RENAME CONSTRAINT "FK_SmallGroup_Church_ChurchId" TO fk_small_group_church_id;
ALTER TABLE pt."SmallGroup" RENAME TO small_group;

ALTER INDEX pt."IX_SmallGroup_ChurchId" RENAME TO ix_small_group_church_id;

-- Time Zone
ALTER TABLE pt."TimeZone" RENAME COLUMN "TimeZoneId" TO id;
ALTER TABLE pt."TimeZone" RENAME COLUMN "Description" TO description;
ALTER TABLE pt."TimeZone" RENAME COLUMN "SortOrder" TO sort_order;
ALTER TABLE pt."TimeZone" RENAME COLUMN "IsActive" TO is_active;
ALTER TABLE pt."TimeZone" RENAME CONSTRAINT "PK_TimeZone" TO pk_time_zone;
ALTER TABLE pt."TimeZone" RENAME TO time_zone;

-- User
ALTER TABLE pt."User" RENAME COLUMN "UserId" TO id;
ALTER TABLE pt."User" RENAME COLUMN "FirstName" TO first_name;
ALTER TABLE pt."User" RENAME COLUMN "LastName" TO last_name;
ALTER TABLE pt."User" RENAME COLUMN "EmailAddress" TO email;
ALTER TABLE pt."User" RENAME COLUMN "IsSystemAdmin" TO is_admin;
ALTER TABLE pt."User" RENAME COLUMN "PasswordHash" TO password_hash;
ALTER TABLE pt."User" RENAME COLUMN "Salt" TO salt;
ALTER TABLE pt."User" RENAME CONSTRAINT "PK_User" TO pk_pt_user;
ALTER TABLE pt."User" RENAME TO pt_user;
ALTER TABLE pt.pt_user ADD COLUMN last_seen timestamptz;

-- User / Small Group
ALTER TABLE pt."User_SmallGroup" RENAME COLUMN "UserId" TO user_id;
ALTER TABLE pt."User_SmallGroup" RENAME COLUMN "SmallGroupId" TO small_group_id;
ALTER TABLE pt."User_SmallGroup" RENAME CONSTRAINT "PK_User_SmallGroup" TO pk_user_small_group;
ALTER TABLE pt."User_SmallGroup" RENAME CONSTRAINT "FK_User_SmallGroup_User_UserId" TO fk_user_small_group_user_id;
ALTER TABLE pt."User_SmallGroup" RENAME CONSTRAINT "FK_User_SmallGroup_SmallGroup_SmallGroupId" TO fk_user_small_group_small_group_id;
ALTER TABLE pt."User_SmallGroup" RENAME TO user_small_group;

ALTER INDEX pt."IX_User_SmallGroup_SmallGroupId" RENAME TO ix_user_small_group_small_group_id;

-- #41 - change to timestamptz
SET TimeZone = 'UTC';
ALTER TABLE pt.prayer_request ALTER COLUMN entered_date TYPE timestamptz;
ALTER TABLE pt.prayer_request ALTER COLUMN updated_date TYPE timestamptz;
