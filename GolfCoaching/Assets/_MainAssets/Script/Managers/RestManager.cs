using UnityEngine;
using Proyecto26;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

public class RestManager : MonoBehaviourSingleton<RestManager>
{
    private readonly string basePath = "http://develop.csdll.co.kr/Baekdori";    
    public string QRLoginUrl;

    HubConnection hubConnection;
    string jwtToken;
    string currentKioskId;
    string currentUserId;
    string currentUserName;
    string currentSessionSeq;
    string currentSessionMode;
    string currentWatchSeq;
    string currentLessonSeq;
    DateTime? sessionStartLocalDt;
    DateTime? nextSessionUpdateLocalDt;
    DateTime? watchStartLocalDt;
    DateTime? nextWatchUpdateLocalDt;
    bool manualHubStop;
    bool hubReconnectInProgress;
    bool sessionUpdateInProgress;
    bool sessionEndInProgress;
    bool watchUpdateInProgress;
    bool watchEndInProgress;
    
    public Action LoginSuccess;
    public KioskLoginData LoginUserData;
    public bool KioskLogin = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        KioskAuth("k1","1234",
        async (ret) =>
        {
            if(string.IsNullOrEmpty(ret) == false)
            {
                Debug.Log("Kiosk Auth Success");
                await ConnectHubAsync("k1");

                
            }
        });
    }

    //로그인 테스트연결 
    public void KioskAuth(string kioskId, string password, Action<string> result)
    {		
        DeviceAuth deviceAuth = new DeviceAuth();
        deviceAuth.kioskId = kioskId;
        deviceAuth.password = password;
        
		RestClient.Post<DeviceAuthRes>(basePath + "/Kiosk/Api/DeviceAuth", deviceAuth)        
		.Then(res =>
        {
            if(res.State.Equals("Success"))
            {
                jwtToken = res.Data.Token;
                RestClient.DefaultRequestHeaders["Authorization"] = $"Bearer {jwtToken}";

                result?.Invoke(kioskId);
            }
            else
            {
                Debug.Log("Faeild ; Kiosk ID or Password not match");
                result?.Invoke(null);
            }
        })
		.Catch(err => {
            Debug.Log("Error : " + err.Message);
            result?.Invoke(null);
        });
	}

    async Task ConnectHubAsync(string kioskId)
    {
        if (hubConnection != null)
        {
            await StopHubAsync(true);
        }

        string hubUrl = basePath + "/kioskHub";//CombineUrl(BaseUrl, "kioskHub");
        int maxRetryCount = 5;
        int retryDelaySeconds = 10;
        if (retryDelaySeconds <= 0)
        {
            retryDelaySeconds = 1;
        }

        //manualHubStop = false;
        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(jwtToken);
            })
            .WithAutomaticReconnect(new LimitedRetryPolicy(maxRetryCount, TimeSpan.FromSeconds(retryDelaySeconds)))
            .Build();

        RegisterHubHandlers(kioskId);

        await hubConnection.StartAsync();
        await JoinKioskGroupAsync(kioskId);

        Debug.Log("Kiosk Hub Joined");

        GetKioskLink(
            (result) =>
            {
                if(result == null)
                    return;

                Debug.Log($"Login QR Link : {result.LoginUrl}");
                QRLoginUrl = result.LoginUrl;
                KioskLogin = true;
            }
        );
    }

    void RegisterHubHandlers(string kioskId)
    {
        hubConnection.On<string>("ReceiveConnectionId", connectionId =>
        {
            Debug.Log("ReceiveConnectionId: " + connectionId);
        });

        hubConnection.On<KioskLoginData>("KioskLogin", login =>
        {
            currentUserId = login.UserId;
            currentUserName = login.UserName;
            Debug.Log("KioskLogin received: " + login.UserId);
            LoginUserData = login;
            
            if (LoginSuccess != null)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(LoginSuccess);
            }
        });

        hubConnection.On<KioskLogoutData>("KioskLogout", logout =>
        {
            Debug.Log("KioskLogout received: " + logout.UserId);
            ClearLoginState();
        });

        hubConnection.Reconnecting += error =>
        {
            Debug.Log("SignalR reconnecting: " + (error?.Message ?? ""));
            return Task.CompletedTask;
        };

        hubConnection.Reconnected += async connectionId =>
        {
            Debug.Log("SignalR reconnected: " + connectionId);
            await JoinKioskGroupAsync(kioskId);
            await ConnectionStateAsync();
        };

        hubConnection.Closed += async error =>
        {
            Debug.Log("SignalR closed: " + (error?.Message ?? ""));

            if (!manualHubStop)
            {
                await HandleHubRetryLimitExceededAsync();
            }
        };
    }

    void GetKioskLink(Action<KioskLinkData> result)
    { 
        RestClient.Post<KioskLinkInfo>(basePath + "/Kiosk/Api/GetKioskLink", null)
        .Then( res =>
        {
            if(res.State.Equals("Success"))
            {
                result?.Invoke(res.Data);
            }
            else
            {
                result?.Invoke(null);
            }
            
        })
        .Catch( response =>
        {
            result?.Invoke(null);
        });
    }

    void PostApi<T>(string relativePath, Action<ApiResponse<T>> result, object payload = null )
    {
        string url = basePath + relativePath;
        //string json = await Post(url, useBearerToken ? jwtToken : null);
        RestClient.Post<ApiResponse<T>>(basePath + relativePath, payload)
        .Then( res =>
        {
            result?.Invoke(res);
        })
        .Catch( res =>
        {
            result?.Invoke(null);
        });
        //return jsonSerializer.Deserialize<ApiResponse<T>>(json);
    }

    async Task ConnectionStateAsync()
    {
        EnsureJwt();

        //ApiResponse<ConnectionStateData> response = await PostApiAsync<ConnectionStateData>("Kiosk/Api/ConnectionState", true);
        PostApi<ConnectionStateData>("Kiosk/Api/ConnectionState", 
        (response) =>
        {
            if(response == null)
                return;

            EnsureSuccess(response, "ConnectionState failed");

            ConnectionStateData data = response.Data;
            //Debug.Log("ConnectionState: " + jsonSerializer.Serialize(data));

            if (data != null)
            {
                currentUserId = data.LoginUserId;

                if (data.HasActiveSession)
                {
                    currentSessionSeq = data.ActiveSessionSeq;
                    currentSessionMode = data.ActiveSessionMode;
                    SetSessionTiming(data.ActiveSessionTime);
                    //StartSessionTimer(false);
                }
                else
                {
                    ClearSessionState();
                }

                if (data.HasActiveWatch)
                {
                    currentWatchSeq = data.ActiveWatchSeq;
                    currentLessonSeq = data.ActiveLessonSeq;
                    //StartWatchTimer(false);
                    //await WatchUpdateAsync();
                }
                else
                {
                    ClearWatchState();
                }
            }   
        });
        
    }

    async Task ConnectionCloseAsync()
    {
        if (jwtToken == null)
        {
            Debug.Log("ConnectionClose skipped. JWT is empty.");
            return;
        }

        //ApiResponse<KioskConnectionCloseData> response = await PostApiAsync<KioskConnectionCloseData>("Kiosk/Api/ConnectionClose", true);
        PostApi<KioskConnectionCloseData>("Kiosk/Api/ConnectionClose",
        (response) =>
        {
            if(response == null)
                return;

            EnsureSuccess(response, "ConnectionClose failed");

            //Debug.Log("ConnectionClose success: " + jsonSerializer.Serialize(response.Data));
            ClearLoginState();
        }
        );
        
    }
    async Task HandleHubRetryLimitExceededAsync()
    {
        if (hubReconnectInProgress) return;

        hubReconnectInProgress = true;
        try
        {
            Debug.Log("Retry limit exceeded. Request ConnectionClose.");
            try
            {
                await ConnectionCloseAsync();
            }
            catch (Exception ex)
            {
                Debug.Log("ConnectionClose after retry limit failed: " + ex.Message);
            }
        }
        finally
        {
            hubReconnectInProgress = false;
        }
    }

    async Task JoinKioskGroupAsync(string kioskId)
    {
        await hubConnection.InvokeAsync("JoinGroup", kioskId, "Waiting");
    }

    async Task StopHubAsync(bool manualStop)
    {
        manualHubStop = manualStop;
        if (hubConnection != null)
        {
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();
            hubConnection = null;
        }
    }


    void ClearLoginState()
    {
        currentUserId = null;
        currentUserName = null;
        ClearSessionState();
    }

    void ClearSessionState()
    {
        //StopSessionTimer();
        ClearWatchState();
        currentSessionSeq = null;
        currentSessionMode = null;
        sessionStartLocalDt = null;
    }

    void ClearWatchState()
    {
        //StopWatchTimer();
        currentWatchSeq = null;
        currentLessonSeq = null;
        watchStartLocalDt = null;
    }

    void SetSessionTiming(int? sessionTime)
    {
        int seconds = sessionTime ?? 0;
        if (seconds < 0) seconds = 0;

        sessionStartLocalDt = DateTime.Now.AddSeconds(-seconds);
    }

    void EnsureJwt()
    {
        if (string.IsNullOrEmpty(jwtToken))
        {
            throw new InvalidOperationException("Connect the kiosk first.");
        }
    }

    void EnsureSession()
    {
        if (string.IsNullOrEmpty(currentSessionSeq))
        {
            throw new InvalidOperationException("Start a kiosk session first.");
        }
    }

    void EnsureWatch()
    {
        if (string.IsNullOrEmpty(currentWatchSeq) || string.IsNullOrEmpty(currentLessonSeq))
        {
            throw new InvalidOperationException("Start a lesson watch first.");
        }
    }

    static void EnsureSuccess<T>(ApiResponse<T> response, string fallbackMessage)
    {
        if (response == null)
        {
            throw new InvalidOperationException(fallbackMessage);
        }

        if (response.State != "Success")
        {
            throw new InvalidOperationException(string.IsNullOrEmpty(response.Message) ? fallbackMessage : response.Message);
        }
    }

    /*
    void WatchStartAsync()
    {
        EnsureJwt();
        EnsureSession();

        //string lessonSeq = txtLessonSeq.Text.Trim();
        if (lessonSeq.Length == 0)
        {
            ShowMessage("LessonSeq is required.");
            return;
        }

        var payload = new
        {
            LessonSeq = lessonSeq,
            SessionMode = currentSessionMode ?? txtSessionMode.Text.Trim(),
            ClientEventId = CreateClientEventId("watch-start")
        };

        ApiResponse<WatchStartData> response = await PostJsonApiAsync<WatchStartData>("Kiosk/Api/WatchStart", payload, true);
        EnsureSuccess(response, "WatchStart failed");

        LessonWatchData entity = response.Data?.entity;
        if (entity == null)
        {
            throw new InvalidOperationException("WatchStart response has no watch entity.");
        }

        currentWatchSeq = entity.WatchSeq;
        currentLessonSeq = entity.LessonSeq;
        //txtWatchSeq.Text = currentWatchSeq;
        SetWatchTiming(entity.WatchTime);
        StartWatchTimer(true);

        Debug.Log("WatchStart success. WatchSeq=" + currentWatchSeq
            + ", AlreadyStarted=" + response.Data.AlreadyStarted);        
    }

    void WatchUpdateAsync()
    {
        EnsureJwt();
        EnsureWatch();

        var payload = new
        {
            WatchSeq = currentWatchSeq,
            LessonSeq = currentLessonSeq,
            ClientEventId = CreateClientEventId("watch-update"),
            WatchTime = CalculateLocalWatchTime()
        };

        ApiResponse<WatchProgressData> response = await PostJsonApiAsync<WatchProgressData>("Kiosk/Api/WatchUpdate", payload, true);
        EnsureSuccess(response, "WatchUpdate failed");

        WatchProgressData data = response.Data;
        if (data == null)
        {
            throw new InvalidOperationException("WatchUpdate response has no progress data.");
        }

        ApplyWatchProgress(data);
        Debug.Log("WatchUpdate success. AlreadyEnded=" + data.AlreadyEnded
            + ", WatchTime=" + (data.entity?.WatchTime ?? 0));

        if (data.AlreadyEnded)
        {
            ClearWatchState();
            return;
        }
    }

    void WatchUpdateFromTimerAsync()
    {
        watchUpdateInProgress = true;

        try
        {
            await WatchUpdateAsync();
        }
        catch (Exception ex)
        {
            Debug.Log("WatchUpdate error: " + ex.Message);
            nextWatchUpdateLocalDt = DateTime.Now.AddSeconds(WatchUpdateIntervalSeconds);
        }
        finally
        {
            watchUpdateInProgress = false;
        }
    }

    void WatchEndAsync()
    {
        if (watchEndInProgress) return;

        EnsureJwt();
        watchEndInProgress = true;
        watchTimer.Stop();

        try
        {
            string watchSeq = txtWatchSeq.Text.Trim();
            string lessonSeq = currentLessonSeq;
            if (string.IsNullOrEmpty(lessonSeq))
            {
                lessonSeq = txtLessonSeq.Text.Trim();
            }

            if (watchSeq.Length == 0 || lessonSeq.Length == 0)
            {
                Debug.Log("WatchSeq and LessonSeq are required.");
                return;
            }

            var payload = new
            {
                WatchSeq = watchSeq,
                LessonSeq = lessonSeq,
                SessionMode = currentSessionMode ?? txtSessionMode.Text.Trim(),
                ClientEventId = CreateClientEventId("watch-end"),
                WatchTime = CalculateLocalWatchTime()
            };

            ApiResponse<WatchProgressData> response = await PostJsonApiAsync<WatchProgressData>("Kiosk/Api/WatchEnd", payload, true);
            EnsureSuccess(response, "WatchEnd failed");

            WatchProgressData data = response.Data;
            if (data == null)
            {
                throw new InvalidOperationException("WatchEnd response has no progress data.");
            }

            Debug.Log("WatchEnd success. AlreadyEnded=" + data.AlreadyEnded
                + ", WatchTime=" + (data.entity?.WatchTime ?? 0));
            ClearWatchState();
        }
        catch
        {
            RestartWatchTimer();
            throw;
        }
        finally
        {
            watchEndInProgress = false;
        }
    }
    */
}

public sealed class LimitedRetryPolicy : IRetryPolicy
{
	readonly int maxRetryCount;
	readonly TimeSpan retryDelay;

	public LimitedRetryPolicy(int maxRetryCount, TimeSpan retryDelay)
	{
		this.maxRetryCount = maxRetryCount;
		this.retryDelay = retryDelay;
	}

	public TimeSpan? NextRetryDelay(RetryContext retryContext)
	{
		if (retryContext.PreviousRetryCount >= maxRetryCount)
		{
			return null;
		}

		return retryDelay;
	}
}

public class KioskLoginData
{
    public string UserId { get; set; }

    public string UserName { get; set; }

    public string SiteId { get; set; }

    public string KioskId { get; set; }

    public long ServerTicks { get; set; }
}

public class KioskLogoutData
{
	public string UserId { get; set; }

	public string UserName { get; set; }

	public string KioskId { get; set; }

	public string Reason { get; set; }

	public long ServerTicks { get; set; }
}