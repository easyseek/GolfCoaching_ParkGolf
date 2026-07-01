using System;
using System.Text;
using UnityEngine;

public class StudioManager : MonoBehaviourSingleton<StudioManager>
{
    #region 변수들
    //TODO : 키워드 생기면 추가 변수 넣어야함.

    /// <summary>
    /// 스튜디오 저장할 영상 제목
    /// </summary>
    [Header("스튜디오 저장할 영상 제목 (초기제목은 추천)")]
    public string studioVideoTitleName;
    
    private StringBuilder videoTitleNameStringBuilder = new StringBuilder();

    /// <summary>
    /// 현재 선택 된 골프 클럽 타입
    /// </summary>
    public ClubType clubType = ClubType.Driver;

    /// <summary>
    /// 골프 클럽 타입
    /// </summary>
    public enum ClubType
    {
        Driver = 0,
        Wood = 1,
        LongIron = 2,
        MiddleIron = 3,
        ShortIron = 4,
        Approach = 5,
        Putter = 6,
    }

    #endregion

    private void Start()
    {
        // 첫 추천 영상 제목을 만듭니다.
        RecommandVideoName(clubType, swingType);
    }

    /// <summary>
    /// 현재 선택된 골프 스윙 타입
    /// </summary>
    public SwingType swingType = SwingType.HalfSwing;

    /// <summary>
    /// 골프 스윙 타입
    /// </summary>
    public enum SwingType
    {
        HalfSwing = 0,
        ThreeQuarterSwing = 1,
        FullSwing = 2,
        Grip = 3,
        Address = 4,
        TakeBack = 5,
        BackSwing = 6,
        DownSwing = 7,
        Impact = 8,
        Follow = 9,
        Finish = 10,
    }

    /// <summary>
    /// 현재 선택된 클럽을 바꿔주는 메서드
    /// </summary>
    /// <param name="index">클럽 enum의 인덱스 번호</param>
    public void ChangeClubType(int index)
    {
        if (Enum.IsDefined(typeof(ClubType), index))
        {
            clubType = (ClubType)index;

            // 영상 추천 제목 변경
            RecommandVideoName(clubType, swingType);
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// 현재 선택된 스윙을 바꿔주는 메서드
    /// </summary>
    /// <param name="index">스윙 enum의 인덱스 번호</param>
    public void ChangeSwingType(int index)
    {
        if (Enum.IsDefined(typeof(SwingType), index))
        {
            swingType = (SwingType)index;

            // 영상 추천 제목 변경
            RecommandVideoName(clubType, swingType);
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// 저장할 영상의 제목을 추천하는 메서드.
    /// </summary>
    /// <param name="clubType">선택된 클럽 타입</param>
    /// <param name="swingType">선택된 스윙 타입</param>
    public void RecommandVideoName(ClubType clubType, SwingType swingType)
    {
        videoTitleNameStringBuilder.Clear();
        videoTitleNameStringBuilder.Append(GetKoreanNameForClub(clubType));
        videoTitleNameStringBuilder.Append(" / ");
        videoTitleNameStringBuilder.Append(GetKoreanNameForSwing(swingType));

        // 추천 영상 제목을 만듭니다.
        studioVideoTitleName = videoTitleNameStringBuilder.ToString();
    }

    /// <summary>
    /// 클럽 타입에 따른 한글 이름을 반환하는 메서드.
    /// </summary>
    /// <param name="clubType">클럽 타입</param>
    /// <returns>한글 이름</returns>
    private string GetKoreanNameForClub(ClubType clubType)
    {
        switch (clubType)
        {
            case ClubType.Driver:
                return "드라이버";
            case ClubType.Wood:
                return "우드";
            case ClubType.LongIron:
                return "롱 아이언";
            case ClubType.MiddleIron:
                return "미들 아이언";
            case ClubType.ShortIron:
                return "쇼트 아이언";
            case ClubType.Approach:
                return "어프로치";
            case ClubType.Putter:
                return "퍼터";
            default:
                return "알 수 없음";
        }
    }

    /// <summary>
    /// 스윙 타입에 따른 한글 이름을 반환하는 메서드.
    /// </summary>
    /// <param name="swingType">스윙 타입</param>
    /// <returns>한글 이름</returns>
    private string GetKoreanNameForSwing(SwingType swingType)
    {
        switch (swingType)
        {
            case SwingType.HalfSwing:
                return "하프 스윙";
            case SwingType.ThreeQuarterSwing:
                return "쓰리 쿼터 스윙";
            case SwingType.FullSwing:
                return "풀 스윙";
            case SwingType.Grip:
                return "그립";
            case SwingType.Address:
                return "어드레스";
            case SwingType.TakeBack:
                return "테이크백";
            case SwingType.BackSwing:
                return "백 스윙";
            case SwingType.DownSwing:
                return "다운 스윙";
            case SwingType.Impact:
                return "임팩트";
            case SwingType.Follow:
                return "팔로우";
            case SwingType.Finish:
                return "피니시";
            default:
                return "알 수 없음";
        }
    }

    /// <summary>
    /// 비디오 이름을 설정하는 메서드
    /// </summary>
    /// <param name="videoName">비디오 이름</param>
    public void SetVideoName(string videoName)
    {
        videoTitleNameStringBuilder.Clear();
        videoTitleNameStringBuilder.Append(videoName);

        // 비디오 이름을 결정합니다.
        studioVideoTitleName = videoTitleNameStringBuilder.ToString();
    }
}
