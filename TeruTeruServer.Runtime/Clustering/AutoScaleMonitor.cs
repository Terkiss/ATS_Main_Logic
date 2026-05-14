using System.Linq;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.Clustering
{
    public class AutoScaleMonitor
    {
        private readonly IClusterRegistry _registry;
        private readonly IEventBus _eventBus;

        // 임계치 (M12)
        private const int SCALE_UP_THRESHOLD = 80;    // 평균 부하 80% 초과 시
        private const int SCALE_DOWN_THRESHOLD = 20;   // 평균 부하 20% 미만 시
        private const int MIN_NODES = 1;
        private const int MAX_CONNECTIONS_PER_NODE = 1000;

        public AutoScaleMonitor(IClusterRegistry registry, IEventBus eventBus)
        {
            _registry = registry;
            _eventBus = eventBus;
        }

        public ScaleDecision Evaluate()
        {
            var nodes = _registry.GetActiveNodes();
            if (nodes.Count == 0) return ScaleDecision.None;

            double avgLoad = nodes.Average(n =>
                (double)n.CurrentConnections / MAX_CONNECTIONS_PER_NODE * 100);

            if (avgLoad > SCALE_UP_THRESHOLD)
                return ScaleDecision.ScaleUp;
            else if (avgLoad < SCALE_DOWN_THRESHOLD && nodes.Count > MIN_NODES)
                return ScaleDecision.ScaleDown;

            return ScaleDecision.None;
        }

        /// <summary>
        /// Tick 핸들러에서 호출. 스케일링 판단 후 이벤트 발행. (M12)
        /// </summary>
        public void CheckAndNotify()
        {
            var decision = Evaluate();
            if (decision != ScaleDecision.None)
            {
                _eventBus.Publish("cluster:scale", decision);
            }
        }
    }

    public enum ScaleDecision { None, ScaleUp, ScaleDown }
}
