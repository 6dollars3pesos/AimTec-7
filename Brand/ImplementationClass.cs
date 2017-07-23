namespace Brand
{
  
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Health;
    using Aimtec.SDK.TargetSelector;

    /// <summary>
    ///     The Implementation class.
    /// </summary>
    internal static class ImplementationClass
    {
        #region Static Fields

        /// <summary>
        ///     Gets the HealthPrediction implementation.
        /// </summary>
        public static IHealthPrediction IHealthPrediction = HealthPrediction.Implementation;

        /// <summary>
        ///     Gets the Orbwalker implementation.
        /// </summary>
        public static IOrbwalker IOrbwalker = Orbwalker.Implementation;

        /// <summary>
        ///     Gets the Prediction implementation.
        /// </summary>
        //public static IPrediction IPrediction = Prediction.Implementation;

        /// <summary>
        ///     Gets the TargetSelector implementation.
        /// </summary>
        public static ITargetSelector ITargetSelector = TargetSelector.Implementation;

        #endregion
    }
}
