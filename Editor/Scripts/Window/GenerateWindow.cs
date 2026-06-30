#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnivelerCode.GpuAnimation.Editor.Utils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    /// <summary>
    /// Editor window for baking SkinnedMeshRenderers and Animator Controllers into GPU-accelerated animation assets.
    /// </summary>
    public sealed class GeneratorWindow : EditorWindow
    {
        [SerializeField] private Shader litShader;
        [SerializeField] private Shader unlitShader;

        private const string _customShaderName = "Custom Animation Shader";
        private const string _guiPath = "Packages/com.sniveler-code.gpu-animation/Editor/Gui/";
        private const string _mainUxml = _guiPath + "GenerateWindow.uxml";
        private const string _lodsUxml = _guiPath + "LodTemplate.uxml";
        private const string _animatorUxml = _guiPath + "AnimationTemplate.uxml";
        private const string _infoUxml = _guiPath + "InfoBox.uxml";

        private const string _errorPrefab = "Select a prefab to set up LOD levels and bake bone data.";
        private const string _errorAnimator = "Select the Animator Controller to modify its parameters and states.";
        private const string _errorShader1 = "No shader selected. The generated asset may not render correctly.";
        private const string _errorShader2 = "Selected shader does not use SnivelerAnimationNode.";
        private const string _proUrl = "https://assetstore.unity.com/packages/tools/animation/gpu-animation-entities-pro-370150";


        /// <summary>
        /// The processor responsible for the actual baking logic.
        /// </summary>
        private GenerateProcessor _mProcessor;

        [MenuItem("Window/Sniveler Code/Animator Baker", false)]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(GeneratorWindow));
            window.titleContent = new GUIContent("Animator Baker");
        }

        private T RootQuery<T>(string elementName) where T : VisualElement => rootVisualElement.Q<T>(elementName);

        private PrefabInstance _instance;
        private VisualElement _modalOverlay;
        private Button _confirmBtn;
        private Action _currentConfirmCallback;
        private ObjectField _animatorField;
        private VisualElement _messageContainer;
        private readonly Dictionary<string, TemplateContainer> _messages = new();
        private Button _generateButton;

        /// <summary>
        /// Initializes the UI using UIElements and loads required shader templates.
        /// </summary>
        public void CreateGUI()
        {
            if (litShader == null && !TryFindShader(AnimatorStrings.LitTemplateName, out litShader))
                ToggleMessageInfo("lit",
                    $"{AnimatorStrings.LitTemplateName} shader not found. " +
                    $"Please ensure the package is correctly imported", 2);

            if (unlitShader == null && !TryFindShader(AnimatorStrings.UnlitTemplateName, out unlitShader))
                ToggleMessageInfo("unlit",
                    $"{AnimatorStrings.UnlitTemplateName} shader not found. " +
                    $"Please ensure the package is correctly imported", 2);

            _mProcessor = new GenerateProcessor();

            VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_mainUxml);
            var template = uiAsset.Instantiate();
            template.style.flexGrow = 1;
            rootVisualElement.Add(template);

            _instance = new PrefabInstance();

            var contentTabs = rootVisualElement.Query<VisualElement>(className: "tab-page").ToList();
            var headerTabs = rootVisualElement.Query<Label>(className: "tab-button").ToList();
            headerTabs.ForEach(label =>
            {
                label.RegisterCallback<PointerUpEvent>(_ =>
                {
                    headerTabs.ForEach(t => t.RemoveFromClassList("tab-button--active"));
                    label.AddToClassList("tab-button--active");

                    int index = headerTabs.IndexOf(label);
                    contentTabs.ForEach(c => c.RemoveFromClassList("tab-page--active"));
                    contentTabs[index].AddToClassList("tab-page--active");
                });
            });

            RootQuery<ObjectField>("PrefabField")
                .RegisterValueChangedCallback(PrefabChange);

            _messageContainer = RootQuery<VisualElement>("MessageContainer");

            _animatorField = RootQuery<ObjectField>("AnimatorField");
            _animatorField.RegisterValueChangedCallback(AnimatorChange);

            _modalOverlay = rootVisualElement.Q<VisualElement>("ModalInstance");
            _modalOverlay.style.display = DisplayStyle.None;
            _modalOverlay.Q<Button>("CancelBtn").clicked += () =>
                _modalOverlay.style.display = DisplayStyle.None;

            _confirmBtn = _modalOverlay.Q<Button>("ConfirmBtn");
            _confirmBtn.clicked += () =>
            {
                _currentConfirmCallback?.Invoke();
                _modalOverlay.style.display = DisplayStyle.None;
            };

            _generateButton = RootQuery<Button>("Generate");
            _generateButton.RegisterCallback<ClickEvent>(_ =>
            {
                AsyncCall(async () =>
                {
                    await _mProcessor.GenerateAnimationAsset(_instance);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                });
            });

            var useDqs = RootQuery<Toggle>("UseDqs");
            useDqs.value = false;
            useDqs.SetEnabled(false);

            var rootMotion = RootQuery<Toggle>("RootMotion");
            rootMotion.value = false;
            rootMotion.SetEnabled(false);

            RootQuery<Button>("GetProp").RegisterCallback<ClickEvent>(_ =>
                Application.OpenURL(_proUrl));

            ToggleMessageInfo("prefab", _errorPrefab, 2);
            ToggleMessageInfo("animator", _errorAnimator, 2);
        }

        private void ToggleMessageInfo(string key, string message, int type = 0)
        {
            if (string.IsNullOrEmpty(message))
            {
                if (_messages.TryGetValue(key, out TemplateContainer el))
                {
                    _messageContainer.Remove(el);
                    _messages.Remove(key);
                }
            }
            else
            {
                if (!_messages.TryGetValue(key, out TemplateContainer template))
                {
                    var infoBox = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_infoUxml);
                    template = infoBox.Instantiate();
                    _messages.Add(key, template);
                    _messageContainer.Add(template);
                }

                template.ClearClassList();
                template.Q<Label>().text = message;
                template.AddToClassList(type switch
                {
                    0 => "info--info",
                    1 => "info--warning",
                    2 => "info--error",
                    _ => "info--info"
                });
            }

            _generateButton.SetEnabled(_messages.Count == 0);
            _messageContainer.style.height = _messages.Count * 38;

            var lodsContainer = RootQuery<VisualElement>("LodsContainer");
            lodsContainer.RemoveFromClassList("active");
            if (!_messages.ContainsKey("prefab")) lodsContainer.AddToClassList("active");

            var bonesContainer = RootQuery<VisualElement>("BonesContainer");
            bonesContainer.RemoveFromClassList("active");
            if (!_messages.ContainsKey("prefab")) bonesContainer.AddToClassList("active");
        }

        private static bool TryFindShader(string shaderName, out Shader shader)
        {
            shader = null;
            string[] guids = AssetDatabase.FindAssets($"{shaderName} t:Shader");
            if (guids.Length == 0) return false;
            shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return shader != null;
        }

        /// <summary>
        /// Helper to run asynchronous tasks within the Editor context.
        /// </summary>
        /// <param name="callback">The async task to execute.</param>
        private static async void AsyncCall(Func<Task> callback)
        {
            try
            {
                await callback();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }


        /// <summary>
        /// Triggered when the source Prefab is changed. Validates the prefab and populates the UI with LOD and Bone data.
        /// </summary>
        /// <param name="evt">The change event containing the new GameObject.</param>
        private void PrefabChange(ChangeEvent<Object> evt)
        {
            _instance.Clear();
            _instance.Source = (GameObject) evt.newValue;

            if (_instance.Source == null)
            {
                ToggleMessageInfo("prefab", _errorPrefab, 2);
                return;
            }

            var skinRenderers = _instance.Source.GetComponentsInChildren<SkinnedMeshRenderer>();

            var message = RootQuery<Label>("ErrorMessage");
            if (skinRenderers == null || skinRenderers.Length == 0)
            {
                ((ObjectField) evt.target).SetValueWithoutNotify(null);
                message.text = "no skinned renderer";
                return;
            }

            ToggleMessageInfo("prefab", null);

            message.text = string.Empty;
            _instance.SetSkins(_instance.Source);

            var animator = _instance.Source.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController)
            {
                if (_instance.Animator != null)
                {
                    string confirmMessage =
                        $"Controller '{animator.runtimeAnimatorController.name}' was found. " +
                        "Do you want to apply it?";
                    ShowConfirm(confirmMessage, () => _animatorField.value =
                        (AnimatorController) animator.runtimeAnimatorController);
                }
                else
                {
                    _animatorField.value = (AnimatorController) animator.runtimeAnimatorController;
                }
            }

            // lods
            var shaderChoices = new List<(Shader value, string name)>();
            if (litShader != null) shaderChoices.Add((litShader, litShader.name));
            if (unlitShader != null) shaderChoices.Add((unlitShader, unlitShader.name));
            shaderChoices.Add((null, _customShaderName));

            var shaderContainerField = RootQuery<VisualElement>("CustomShaderContainer");

            var shaderObjectField = RootQuery<ObjectField>("ShaderField");
            shaderObjectField.objectType = typeof(Shader);
            shaderObjectField.value = shaderChoices[0].value;
            shaderObjectField.RegisterValueChangedCallback(shaderEvent =>
            {
                var shader = (Shader) shaderEvent.newValue;
                if (shader == null)
                {
                    ToggleMessageInfo("shader", _errorShader1, 2);
                    return;
                }

                if (!IsAnimationNodeUsed(shader))
                {
                    ToggleMessageInfo("shader", _errorShader2, 2);
                    return;
                }

                ToggleMessageInfo("shader", null);
                _instance.Shader = shader;
            });

            var shaderTypeField = RootQuery<DropdownField>("ShaderType");
            shaderTypeField.choices = shaderChoices.Select(a => a.name).ToList();
            shaderTypeField.RegisterValueChangedCallback(shaderEvent =>
            {
                if (shaderEvent.newValue != _customShaderName)
                {
                    var selection = shaderChoices.FirstOrDefault(a => a.name == shaderEvent.newValue);
                    shaderObjectField.value = selection.value;
                    shaderContainerField.style.display = DisplayStyle.None;
                }
                else
                {
                    shaderContainerField.style.display = DisplayStyle.Flex;
                }
            });

            shaderTypeField.index = 0;

            var lodsContent = RootQuery<VisualElement>("LodsContent");
            lodsContent.Clear();

            for (int i = 0; i < _instance.Lods.Count; i++)
            {
                RenderLodElement(_instance.Lods[i], i, lodsContent);
            }

            // bones
            _instance.Bones.Clear();
        }

        private void ShowConfirm(string message, Action onConfirm)
        {
            _currentConfirmCallback = onConfirm;
            _modalOverlay.Q<Label>("ModalText").text = message;
            _modalOverlay.style.display = DisplayStyle.Flex;
        }

        private static bool IsAnimationNodeUsed(Shader shader)
        {
            if (shader.FindPropertyIndex(AnimatorStrings.RenderFrames) == -1) return false;
            return true;
        }

        private void RenderLodElement(LodInstance lodInstance, int lodIndex, VisualElement content)
        {
            var lodTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_lodsUxml)
                .Instantiate();

            lodTemplate.Q<Label>(className: "unity-button").text = $"LOD {lodIndex}";

            var percentField = lodTemplate.Q<SliderInt>("Percent");
            percentField.value = lodInstance.Percent;
            percentField.RegisterValueChangedCallback(evt => lodInstance.Percent = evt.newValue);

            string[] qualityNames = Enum.GetNames(typeof(LodInstance.Quality));
            var dropdown = lodTemplate.Q<DropdownField>("Quality");
            dropdown.choices = qualityNames.ToList();
            dropdown.value = qualityNames[0];
            dropdown.SetEnabled(false);
            dropdown.tooltip = "Pro Version required";

            var interpolationField = lodTemplate.Q<Toggle>("Interpolation");
            interpolationField.value = false;
            interpolationField.SetEnabled(false);
            interpolationField.tooltip = "Pro Version required";

            var shadowsField = lodTemplate.Q<Toggle>("Shadows");
            shadowsField.value = true;
            shadowsField.SetEnabled(false);
            shadowsField.tooltip = "Pro Version required";

            content.Add(lodTemplate);
        }

        private void AnimatorChange(ChangeEvent<Object> evt)
        {
            var animator = (AnimatorController) evt.newValue;
            var animationsContainer = RootQuery<VisualElement>("AnimationsContainer");
            animationsContainer.style.display = animator ? DisplayStyle.Flex : DisplayStyle.None;
            animationsContainer.Clear();

            RootQuery<Button>("Generate").style.display =
                animator ? DisplayStyle.Flex : DisplayStyle.None;

            ToggleMessageInfo("animator", animator == null ? _errorAnimator : null, 2);

            if (!animator) return;

            _instance.SetAnimator(animator);
            _instance.Clips.Clear();

            AnimatorStateMachine stateMachine = animator.layers[0].stateMachine;
            foreach (ChildAnimatorState animatorState in stateMachine.states)
            {
                AnimatorState state = animatorState.state;
                var clipInstance = new ClipInstance {Enable = true, Fps = 60, Speed = 1f, StateName = state.name};

                var animationTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_animatorUxml)
                    .Instantiate().Q<VisualElement>("RootElement");

                animationTemplate.Q<Label>("ClipName").text = state.name;
                animationTemplate.Q<Toggle>("ClipEnabled").RegisterValueChangedCallback(toggleEvent =>
                    clipInstance.Enable = toggleEvent.newValue);

                var sliderFps = animationTemplate.Q<SliderInt>("ClipFps");
                sliderFps.value = clipInstance.Fps;
                sliderFps.RegisterValueChangedCallback(fpsEvent => clipInstance.Fps = fpsEvent.newValue);

                animationsContainer.Add(animationTemplate);
                _instance.Clips.Add(clipInstance);
            }
        }
    }
}

#endif
