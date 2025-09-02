using AutoGenEnums;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 액션 처리를 담당하는 제네릭 핸들러
    /// </summary>
    public class ActionHandler<TAction> where TAction : struct, Enum
    {
        private readonly Dictionary<TAction, string> _actionMap;
        private readonly Func<TAction> _randomGenerator;
        private readonly TAction _randomActionFlag;
        private readonly HashSet<string> _commonActions;

        public ActionHandler(
            Dictionary<TAction, string> actionMap,
            Func<TAction> randomGenerator,
            TAction randomActionFlag,
            HashSet<string> commonActions)
        {
            _actionMap = actionMap;
            _randomGenerator = randomGenerator;
            _randomActionFlag = randomActionFlag;
            _commonActions = commonActions;
        }

        public TAction GetRandomAction() => _randomGenerator();

        public string GetMotionName(TAction action)
        {
            // RANDOM 액션 처리
            if (action.Equals(_randomActionFlag))
                action = GetRandomAction();

            return _actionMap.TryGetValue(action, out string name) ? name : null;
        }

        public bool IsCommonAction(TAction action) =>
            _commonActions.Contains(action.ToString());

        public string GetActionName(TAction action)
        {
            if (IsCommonAction(action))
                return action.ToString();

            return _actionMap.TryGetValue(action, out string name) ? name : action.ToString();
        }
    }
}