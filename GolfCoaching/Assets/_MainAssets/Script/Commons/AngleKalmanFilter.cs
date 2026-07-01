public class AngleKalmanFilter
{
        // === 칼만 필터 상태 변수 (1차원) ===
    // X: 현재 추정 상태 (각도, 0-360)
    public double StateEstimate { get; private set; }
    // P: 추정 오차 공분산
    public double EstimateCovariance { get; private set; }

    // === 필터 파라미터 ===
    // Q: 프로세스 노이즈 공분산 (모델의 불확실성)
    private readonly double ProcessNoiseCovariance;
    // R: 측정 노이즈 공분산 (측정 센서의 불확실성)
    private readonly double MeasurementNoiseCovariance;

    /// <summary>
    /// **[추가된 기본 생성자]**
    /// 기본값 (각도: 0, 초기 공분산: 1.0, Q: 0.001, R: 0.1)으로 칼만 필터를 초기화합니다.
    /// </summary>
    public AngleKalmanFilter()
        : this(0.0, 1.0, 0.001, 0.1) // 4개 파라미터 생성자 체이닝
    {
    }

    /// <summary>
    /// **[기존 생성자]**
    /// 칼만 필터 초기화
    /// </summary>
    /// <param name="initialEstimate">초기 추정 상태 (각도)</param>
    /// <param name="initialCovariance">초기 추정 오차 공분산</param>
    /// <param name="q">프로세스 노이즈 (Q)</param>
    /// <param name="r">측정 노이즈 (R)</param>
    public AngleKalmanFilter(double initialEstimate, double initialCovariance, double q, double r)
    {
        StateEstimate = NormalizeAngle(initialEstimate);
        EstimateCovariance = initialCovariance;
        ProcessNoiseCovariance = q;
        MeasurementNoiseCovariance = r;
    }

    // --- 핵심 순환성 처리 메서드 ---

    /// <summary>
    /// 두 각도 사이의 가장 짧은 차이를 계산합니다.
    /// 결과는 항상 [-180, 180] 범위 내에 있습니다.
    /// </summary>
    private double CalculateShortestAngleDifference(double targetAngle, double sourceAngle)
    {
        double diff = targetAngle - sourceAngle;

        // 1. 차이를 -360 ~ 360 범위로 정규화
        diff %= 360.0;

        // 2. 차이를 -180 ~ 180 범위로 조정
        if (diff > 180.0)
        {
            diff -= 360.0;
        }
        else if (diff < -180.0)
        {
            diff += 360.0;
        }

        return diff;
    }

    /// <summary>
    /// 각도를 0 ~ 360 범위로 정규화합니다.
    /// </summary>
    private double NormalizeAngle(double angle)
    {
        // C# Modulo 연산자는 피연산자의 부호를 따르므로, 양수로 만드는 추가적인 조작 필요
        double normalized = angle % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }
        return normalized;
    }

    // --- 칼만 필터 주요 단계 ---

    /// <summary>
    /// 필터의 상태를 업데이트하고 새로운 측정값을 통합합니다.
    /// </summary>
    /// <param name="measurement">새로운 각도 측정값 (0-360)</param>
    public double Update(double measurement)
    {
        // 1. 예측 (Prediction) 단계
        // 단일 상태 모델에서는 StateEstimate가 이전 값에서 Q만큼의 노이즈만 반영하여 다음 예측값(X_k^-)이 됨
        // P_k^- = P_k-1 + Q
        EstimateCovariance += ProcessNoiseCovariance;

        // 2. 업데이트 (Update) 단계

        // 2a. 측정 혁신 (Innovation) 계산
        // 순환성 처리: 측정값과 예측값 사이의 가장 짧은 각도 차이(tilde_y) 계산
        double innovation = CalculateShortestAngleDifference(measurement, StateEstimate);

        // 2b. 혁신 공분산 (S) 계산
        // S_k = P_k^- + R
        double innovationCovariance = EstimateCovariance + MeasurementNoiseCovariance;

        // 2c. 칼만 이득 (K) 계산
        // K_k = P_k^- / S_k
        double kalmanGain = EstimateCovariance / innovationCovariance;

        // 2d. 상태 업데이트
        // X_k = X_k^- + K_k * tilde_y
        StateEstimate += kalmanGain * innovation;

        // 업데이트된 상태를 0 ~ 360 범위로 정규화
        StateEstimate = NormalizeAngle(StateEstimate);

        // 2e. 추정 오차 공분산 업데이트
        // P_k = (1 - K_k) * P_k^-
        EstimateCovariance = (1 - kalmanGain) * EstimateCovariance;

        return StateEstimate;
    }
}