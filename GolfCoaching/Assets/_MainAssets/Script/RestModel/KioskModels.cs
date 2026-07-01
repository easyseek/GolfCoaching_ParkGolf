using System;

[Serializable]
public class ApiResponse<T>
{
    public string State { get; set; }

    public string Message { get; set; }

    public T Data { get; set; }
}

[Serializable]
public class DeviceAuth
{
    public string kioskId;
    public string password;
}

[Serializable]
public class DeviceAuthRes
{
    public string State;
    public string Message;    
    public DeviceAuthData Data;
    public string Validation;
}

[Serializable]
public class DeviceAuthData
{
    public string Token;
    public string ExpiresIn;
    public string KioskId;
    public string SiteId;
}

[Serializable]
public class KioskLinkInfo
{
	//public string Hash { get; set; }
    public string State;
    public KioskLinkData Data;
}

[Serializable]
public class KioskLinkData
{
    public string Hash;
    public string LoginUrl;
}


[Serializable]
public class ConnectionStateData
{
	public string KioskId { get; set; }

	public string SiteId { get; set; }

	public string KioskState { get; set; }

	public string LoginUserId { get; set; }

	public DateTime? LoginDt { get; set; }

	public bool HasLogin { get; set; }

	public bool HasActiveSession { get; set; }

	public string ActiveSessionSeq { get; set; }

	public string ActiveSessionMode { get; set; }

	public int? ActiveSessionTime { get; set; }

	public DateTime? ActiveSessionStartDt { get; set; }

	public bool HasActiveWatch { get; set; }

	public string ActiveWatchSeq { get; set; }

	public string ActiveLessonSeq { get; set; }

	public int? ActiveWatchTime { get; set; }

	public DateTime? ActiveWatchStartDt { get; set; }

	public int? RemainAmt { get; set; }

	public int TailGraceSeconds { get; set; }

	public int PlayableSeconds { get; set; }

	public long ServerTicks { get; set; }
}

[Serializable]
public class KioskConnectionCloseData
{
	public string KioskId { get; set; }

	public string SessionSeq { get; set; }

	public bool SessionEnded { get; set; }

	public bool SessionAlreadyEnded { get; set; }

	public bool WatchEnded { get; set; }

	public bool KioskLoggedOut { get; set; }

	public bool KioskPushed { get; set; }
}

[Serializable]
public class WatchStartData
{
	public LessonWatchData entity { get; set; }

	public bool AlreadyStarted { get; set; }

	public long ServerTicks { get; set; }
}

[Serializable]
public class WatchProgressData
{
	public LessonWatchData entity { get; set; }

	public bool AlreadyEnded { get; set; }

	public long ServerTicks { get; set; }
}

[Serializable]
public class LessonWatchData
{
	public string WatchSeq { get; set; }

	public string MemberUserId { get; set; }

	public string SiteId { get; set; }

	public string KioskId { get; set; }

	public string LessonSeq { get; set; }

	public string SessionMode { get; set; }

	public int? WatchTime { get; set; }

	public DateTime? StartDt { get; set; }

	public DateTime? EndDt { get; set; }

	public string WatchState { get; set; }
}