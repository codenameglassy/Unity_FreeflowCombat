namespace FirstGearGames.SmoothCameraShaker
{
    /// <summary>
    /// ShakableTransform2D is currently the exact same as ShakableTransform. Using inheritance for now should I want to expand upon ShakableTransform2D later.
    /// </summary>
    public class ShakableTransform2D : ShakableTransform
    {
        protected override void Awake() { base.Awake(); }
        protected override void OnEnable() { base.OnEnable(); }
        protected override void OnDisable() { base.OnDisable(); }
        protected override void OnBecameInvisible() { base.OnBecameInvisible(); }
        protected override void OnBecameVisible() { base.OnBecameVisible(); }
    }
}