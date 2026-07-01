public class KalmanFilter
{
    private float Q = 0.01f;
    private float R = 0.1f;
    private float P = 1, X = 0, K;

    public float Update(float measurement)
    {
        K = (P + Q) / (P + Q + R);
        X = X + K * (measurement - X);
        P = (1 - K) * (P + Q);
        return X;
    }

    public void Reset()
    {
        P = 1;
        X = 0;
    }
}