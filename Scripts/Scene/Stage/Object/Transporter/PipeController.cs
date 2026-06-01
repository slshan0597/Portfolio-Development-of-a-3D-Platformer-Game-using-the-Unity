namespace Stage
{
    public interface IPipeController : global::IPipeController { }

    public class PipeController : global::PipeController, IRewardable    // Rewardable 속성 추가
    {
        public IPlanetController planet { get; protected set; }

        protected override void SetField()
        {
            base.SetField();

            planet = GetComponentInParent<IPlanetController>(true);
        }
    }
}
