using System.Linq;
using nadena.dev.ndmf;
using Triturbo.FaceTrackingFramework.Runtime;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(YuKoike.EditorExtensions.LilEmoTriturboFTPlugin))]

namespace YuKoike.EditorExtensions
{
    /// <summary>
    /// lilEmoとTriturboFTを統合するNDMFプラグイン
    /// lilEmoのコントローラー生成後に、FacialExpressionsDisabled条件を追加します
    /// </summary>
    public class LilEmoTriturboFTPlugin : Plugin<LilEmoTriturboFTPlugin>
    {
        public override string QualifiedName => "jp.yukoike.lilemo-triturbo-integration";
        public override string DisplayName => "lilEmo × TriturboFT Integration";

        protected override void Configure()
        {
            // Modular Avatarのマージ完了後に実行
            // TriturboFTはGeneratingフェーズで実行されるため、Transformingフェーズでは既にコンポーネントが削除されている
            // そのため、FacialExpressionsDisabledパラメータの存在で判断する
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run("Add FacialExpressionsDisabled to lilEmo", ctx =>
                {
                    ProcessAvatar(ctx);
                });
        }

        private void ProcessAvatar(BuildContext context)
        {
            // VRCAvatarDescriptorを取得
            var descriptor = context.AvatarDescriptor;
            if (descriptor == null)
            {
                return;
            }

            // FXレイヤーのAnimatorControllerを取得
            var fxLayer = descriptor.baseAnimationLayers.FirstOrDefault(l => l.type == VRCAvatarDescriptor.AnimLayerType.FX);
            if (fxLayer.animatorController == null)
            {
                return;
            }

            var controller = fxLayer.animatorController as AnimatorController;
            if (controller == null)
            {
                return;
            }

            // デバッグ: 存在するレイヤーをログ出力
            var layerNames = string.Join(", ", controller.layers.Select(l => l.name));
            Debug.Log($"[LilEmoTriturboFT] Found {controller.layers.Length} layers in FX controller: {layerNames}");

            // lilEmoレイヤーを検索
            AnimatorControllerLayer lilEmoLayer = default;
            bool foundLilEmo = false;

            foreach (var layer in controller.layers)
            {
                if (layer.name == "lilEmo")
                {
                    lilEmoLayer = layer;
                    foundLilEmo = true;
                    break;
                }
            }

            if (!foundLilEmo || lilEmoLayer.stateMachine == null)
            {
                // lilEmoレイヤーが存在しない場合はスキップ
                Debug.Log("[LilEmoTriturboFT] lilEmo layer not found. Integration skipped.");
                return;
            }

            var rootStateMachine = lilEmoLayer.stateMachine;

            // FacialExpressionsDisabledパラメータが存在するか確認
            const string paramName = "FacialExpressionsDisabled";
            bool hasFacialExpressionsDisabledParam = controller.parameters.Any(p => p.name == paramName);

            if (!hasFacialExpressionsDisabledParam)
            {
                // FacialExpressionsDisabledパラメータが存在しない = TriturboFTが使用されていない
                Debug.Log("[LilEmoTriturboFT] FacialExpressionsDisabled parameter not found. TriturboFT integration not needed.");
                return;
            }

            Debug.Log("[LilEmoTriturboFT] Found lilEmo layer and FacialExpressionsDisabled parameter. Adding integration...");

            // Menu、GestureRight、GestureLeftのStateMachineを検索
            var menuStateMachine = rootStateMachine.stateMachines.FirstOrDefault(s => s.stateMachine.name == "Menu").stateMachine;
            var gestureRightStateMachine = rootStateMachine.stateMachines.FirstOrDefault(s => s.stateMachine.name == "GestureRight").stateMachine;
            var gestureLeftStateMachine = rootStateMachine.stateMachines.FirstOrDefault(s => s.stateMachine.name == "GestureLeft").stateMachine;

            int totalModified = 0;

            // 各StateMachineへのEntry遷移に条件を追加
            if (menuStateMachine != null)
            {
                totalModified += AddConditionToEntryTransitions(rootStateMachine, menuStateMachine, paramName);
            }

            if (gestureRightStateMachine != null)
            {
                totalModified += AddConditionToEntryTransitions(rootStateMachine, gestureRightStateMachine, paramName);
            }

            if (gestureLeftStateMachine != null)
            {
                totalModified += AddConditionToEntryTransitions(rootStateMachine, gestureLeftStateMachine, paramName);
            }

            // Idle/Neutralステートからの遷移にも条件を追加
            foreach (var childState in rootStateMachine.states)
            {
                if (childState.state.name == "Neutral" || childState.state.name == "Idle")
                {
                    totalModified += AddConditionToStateTransitions(childState.state, paramName);
                }
            }

            Debug.Log($"[LilEmoTriturboFT] Successfully modified {totalModified} transitions.");
        }

        /// <summary>
        /// rootStateMachineからsubStateMachineへのEntry遷移にFacialExpressionsDisabled条件を追加
        /// </summary>
        private int AddConditionToEntryTransitions(AnimatorStateMachine rootStateMachine, AnimatorStateMachine subStateMachine, string paramName)
        {
            int count = 0;

            foreach (var transition in rootStateMachine.entryTransitions)
            {
                if (transition.destinationStateMachine != subStateMachine)
                {
                    continue;
                }

                // 既に条件が存在する場合はスキップ
                if (transition.conditions.Any(c => c.parameter == paramName))
                {
                    continue;
                }

                // FacialExpressionsDisabled == False の条件を追加
                var conditions = transition.conditions.ToList();
                conditions.Add(new AnimatorCondition
                {
                    mode = AnimatorConditionMode.IfNot,
                    parameter = paramName,
                    threshold = 0
                });
                transition.conditions = conditions.ToArray();
                count++;

                Debug.Log($"[LilEmoTriturboFT] Added condition to: Entry -> {subStateMachine.name} (conditions: {transition.conditions.Length})");
            }

            return count;
        }

        /// <summary>
        /// ステートからの遷移にFacialExpressionsDisabled条件を追加
        /// </summary>
        private int AddConditionToStateTransitions(AnimatorState state, string paramName)
        {
            int count = 0;

            foreach (var transition in state.transitions)
            {
                // 既に条件が存在する場合はスキップ
                if (transition.conditions.Any(c => c.parameter == paramName))
                {
                    continue;
                }

                // GestureLeft/GestureRight/lilEmoパラメータを使用する遷移のみ処理
                var hasTargetParameter = transition.conditions.Any(c =>
                    c.parameter == "lilEmo" ||
                    c.parameter == "GestureLeft" ||
                    c.parameter == "GestureRight");

                if (!hasTargetParameter)
                {
                    continue;
                }

                // FacialExpressionsDisabled == False の条件を追加
                var conditions = transition.conditions.ToList();
                conditions.Add(new AnimatorCondition
                {
                    mode = AnimatorConditionMode.IfNot,
                    parameter = paramName,
                    threshold = 0
                });
                transition.conditions = conditions.ToArray();
                count++;

                Debug.Log($"[LilEmoTriturboFT] Added condition to: {state.name} -> {transition.destinationState?.name ?? transition.destinationStateMachine?.name ?? "Exit"}");
            }

            return count;
        }
    }
}
