using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class FixAnimatorScript
{
    public static void Execute()
    {
        string[] paths = { "Assets/Battle/PlayerAnim.controller", "Assets/Battle/RanPla.controller" };
        foreach(var path in paths)
        {
            AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ac == null) continue;

            foreach(var layer in ac.layers)
            {
                foreach(var state in layer.stateMachine.states)
                {
                    if (state.state.name == "New State")
                    {
                        state.state.name = "Idle";
                    }

                    foreach(var trans in state.state.transitions)
                    {
                        if (trans.hasExitTime && trans.exitTime == 0f)
                        {
                            trans.exitTime = 1.0f;
                            trans.duration = 0.0f;
                        }
                    }
                }
                
                // If there's an Idle state, set it as default
                foreach(var state in layer.stateMachine.states)
                {
                    if (state.state.name == "Idle")
                    {
                        layer.stateMachine.defaultState = state.state;
                        break;
                    }
                }
            }
            EditorUtility.SetDirty(ac);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Animators Fixed by script-execute!");
    }
}
