using UnityEngine;

public enum SimPhase { Chaos, Exploding, Converging, Collapsing }

public class PhaseController : MonoBehaviour
{
    [Header("Durations")]
    public float explodeDuration = 1.2f;
    public float convergingDuration = 9f;
    public float collapsingDuration = 0.7f;  

    [Header("Convergence Curves")]
    public AnimationCurve attractorCurve = AnimationCurve.EaseInOut(0, 0.05f, 1, 0.92f);
    public AnimationCurve temperatureCurve = AnimationCurve.EaseInOut(0, 1f, 1, 0.06f);

    public float WordDensity { get; set; } = 0f;
    public float ChaosTemperature { get; private set; } = 1f;
    public float AttractorWeight { get; private set; } = 0f;
    public float GlowMultiplier { get; private set; } = 1f; 
    public SimPhase CurrentPhase { get; private set; } = SimPhase.Chaos;
    public bool CurrentTargetIsAmine { get; private set; } = true;

    public System.Action<bool> OnConvergingStarted;
    public System.Action OnExplodeStarted;
    public System.Action OnCollapseStarted;   
    public System.Action OnCollapseExplode;    

    private float phaseTimer = 0f;
    private bool _collapseExplode = false;

    void Start() => EnterChaos();

    void Update()
    {
        phaseTimer += Time.deltaTime;

        switch (CurrentPhase)
        {
            case SimPhase.Chaos:
                ChaosTemperature = 1f;
                AttractorWeight = 0f;
                break;

            case SimPhase.Exploding:
                ChaosTemperature = 1f;
                AttractorWeight = 0f;
                if (phaseTimer >= explodeDuration)
                {
                    if (_collapseExplode)
                    {
                        _collapseExplode = false;
                        CurrentPhase = SimPhase.Chaos;
                        phaseTimer = 0f;
                    }
                    else
                    {
                        EnterConverging();
                    }
                }
                break;

            case SimPhase.Converging:
                float t = Mathf.Clamp01(phaseTimer / convergingDuration);
                AttractorWeight = attractorCurve.Evaluate(t);
                ChaosTemperature = temperatureCurve.Evaluate(t);
                GlowMultiplier = 2f;
                float minVisibleTime = convergingDuration * 0.6f;
                if (phaseTimer >= convergingDuration && WordDensity >= 0.72f && phaseTimer >= minVisibleTime)
                    EnterCollapsing();
                else if (phaseTimer >= convergingDuration * 2f)
                    EnterCollapsing();
                break;

            case SimPhase.Collapsing:

                float tc = Mathf.Clamp01(phaseTimer / collapsingDuration);
                AttractorWeight = Mathf.Lerp(0.92f, 0f, tc);
                ChaosTemperature = Mathf.Lerp(0.06f, 0.2f, tc);
                GlowMultiplier = Mathf.Lerp(1f, 0f, tc * tc);  
                if (phaseTimer >= collapsingDuration)
                {
                    GlowMultiplier = 0f;
                    OnCollapseExplode?.Invoke();   
                    CurrentPhase = SimPhase.Exploding;
                    _collapseExplode = true;
                    phaseTimer = 0f;
                    GlowMultiplier = 1f;
                }
                break;
        }
    }

    public void ForceConverge()
    {
        CurrentTargetIsAmine = (Random.value < 0.95f);
        CurrentPhase = SimPhase.Exploding;
        phaseTimer = 0f;
        OnExplodeStarted?.Invoke();
    }

    void EnterConverging()
    {
        CurrentPhase = SimPhase.Converging;
        phaseTimer = 0f;
        OnConvergingStarted?.Invoke(CurrentTargetIsAmine);
    }

    void EnterCollapsing()
    {
        CurrentPhase = SimPhase.Collapsing;
        phaseTimer = 0f;
        OnCollapseStarted?.Invoke();
    }

    void EnterChaos()
    {
        CurrentPhase = SimPhase.Chaos;
        phaseTimer = 0f;
        ChaosTemperature = 1f;
        AttractorWeight = 0f;
        GlowMultiplier = 1f;
    }


    public void InjectChaos(float amount)
    {
        if (CurrentPhase != SimPhase.Converging) return;
        AttractorWeight = Mathf.Max(0f, AttractorWeight - amount * 0.25f);
        ChaosTemperature = Mathf.Min(1f, ChaosTemperature + amount * 0.35f);

    }
}