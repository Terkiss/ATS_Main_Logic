namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 플러그인 간의 의존성 관계를 정의하는 인터페이스입니다.
    /// </summary>
    public interface IPluginDependency
    {
        /// <summary>
        /// 플러그인의 고유 이름입니다.
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// 이 플러그인이 의존하는 다른 플러그인 이름의 배열입니다.
        /// </summary>
        string[] DependsOn { get; }
    }
}
