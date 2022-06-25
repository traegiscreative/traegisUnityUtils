using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    /*--------------------------------------------------------------------------------------------------*/
    /*
                                UI BUTTON - Traegis Creative

    Script for a useful UI Button that can perform multiple hover behaviours with pointer inputs.

    --> This script can easily be modified to account for custom behaviours based on your needs.
    --> Add button sounds easily by modifying 'GetSound(Sound sound)' to fit your project.

    FUNCTIONALITIES:
    --> Access to Actions for all main pointer behaviours
    --> Multiple custom hover behaviours
            - Add to 'buttonPropertyList'
            - 'Hover' type will cause Button to react on pointer enter
            - 'ClickToggle' type will cause Button to react with pointer click toggle
    --> Easily modifiable to add sound behaviours
    --> Any material color manipulation is done through "_Color" property in material. This is up to the user to develop.

    */
    /*--------------------------------------------------------------------------------------------------*/


    //Actions for external application:
    public Action ClickFunc = null;
    public Action MouseRightClickFunc = null;
    public Action MouseMiddleClickFunc = null;
    public Action MouseDownOnceFunc = null;
    public Action MouseUpFunc = null;
    public Action MouseOverOnceFunc = null;
    public Action MouseOutOnceFunc = null;
    public Action MouseOverFunc = null;
    public Action MouseOverPerSecFunc = null; //Triggers every sec if mouseOver
    public Action MouseUpdate = null;
    public Action<PointerEventData> OnPointerClickFunc;
    public Action OnLongPressFunc = null;
    public Action OnLongPressUpFunc = null;
    public Action hoverBehaviourFunc_Enter = null;
    public Action hoverBehaviourFunc_Exit = null;
    public Action OnClickToggleFunc = null;


    [Header("Specify Hover Behaviours:")]
    //Hover Behaviour List:
    [SerializeField] private List<ButtonProperty> buttonPropertyList;


    [Header("Objects to manipulate through Button (Not required):")]
    //Gather for external controls:
    [SerializeField] private RectTransform tooltipObject;
    [SerializeField] private Image mainImage;
    [SerializeField] private TextMeshProUGUI text;

    //Cache:
    private float mouseOverPerSecFuncTimer;
    private Material defaultMaterial;
    private RectTransform rect;

    [Header("General Button Behaviour Settings:")]
    //Settings:
    [SerializeField] private bool resetColorOnClick = true;
    [SerializeField] private bool triggerMouseOutFuncOnClick = false;
    [SerializeField] private bool isClickToggler = false;


    //Toggles:
    private bool isMouseDown;
    private bool isLongPress;
    private bool longPressedExecuted;
    private  bool mouseOver;
    private bool clickToggle;


    //Static:
    public static UI_Button currentlySelectedButton;


    //Sounds:
    public enum Sound
    {
        Null,

        Positive1,
        Positive2,
        Positive3,
        Positive4,
        Positive5,

        Negative1,
        Negative2,
        Negative3,
        Negative4,

        Hover1,
        Hover2,
        Hover3,
        Hover4,
    }

    [Header("Specify Sounds if Enabled:")]
    [SerializeField] private bool doAutoSounds = true;

    [SerializeField] private Sound hoverSound;
    [SerializeField] private Sound positiveSound;
    [SerializeField] private Sound negativeSound;

    private AudioClip hoverClip;
    private AudioClip positiveClip;
    private AudioClip negativeClip;


    /*-------------------------------------------------------------------------------------------------------------------*/


    //MONOBEHAVIOUR METHODS:
    private void Awake()
    {
        if (mainImage == null)
            mainImage = GetComponent<Image>();

        rect = GetComponent<RectTransform>();

        if (doAutoSounds)
        {
            if (hoverSound == Sound.Null)
            {
                hoverSound = Sound.Hover1;
            }
            if (positiveSound == Sound.Null)
            {
                positiveSound = Sound.Positive1;
            }
            if (negativeSound == Sound.Null)
            {
                negativeSound = Sound.Negative1;
            }
        }

        ButtonPropertyRefresh(ButtonProperty.Type.Hover);
        ButtonPropertyRefresh(ButtonProperty.Type.ClickToggle);
    }

    void Start()
    {
        SetSounds();

        defaultMaterial = mainImage.material;
    }

    private void Update()
    {
        if (mouseOver)
        {
            if (MouseOverFunc != null) MouseOverFunc();
            mouseOverPerSecFuncTimer -= Time.unscaledDeltaTime;
            if (mouseOverPerSecFuncTimer <= 0)
            {
                mouseOverPerSecFuncTimer += 1f;
                if (MouseOverPerSecFunc != null) MouseOverPerSecFunc();
            }
        }

        if (MouseUpdate != null) MouseUpdate();
    }



    //POINTER BEHAVIOUR METHODS:
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerEnter != this.gameObject)
            return;

        if (hoverBehaviourFunc_Enter != null) hoverBehaviourFunc_Enter();

        if (mouseOver == false)
            if (MouseOverOnceFunc != null) MouseOverOnceFunc();

        mouseOver = true;
        mouseOverPerSecFuncTimer = 0f;

        if (hoverSound != Sound.Null)
        {
            //Insert code to play sound.
        }

        ButtonPropertyRefresh(ButtonProperty.Type.Hover);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerEnter != this.gameObject)
            return;

        if (hoverBehaviourFunc_Exit != null) hoverBehaviourFunc_Exit();

        if (mouseOver == true)
            if (MouseOutOnceFunc != null) MouseOutOnceFunc();

        mouseOver = false;

        ButtonPropertyRefresh(ButtonProperty.Type.Hover);
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (resetColorOnClick)
            ResetHover();

        //Long press:
        if (longPressedExecuted)
        {
            longPressedExecuted = false;
            return;
        }

        if (OnPointerClickFunc != null) OnPointerClickFunc(eventData);

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (triggerMouseOutFuncOnClick)
            {
                OnPointerExit(eventData);
            }
            if (ClickFunc != null)
            {
                ClickFunc();
            }
        }

        if (eventData.button == PointerEventData.InputButton.Right)
            if (MouseRightClickFunc != null) MouseRightClickFunc();
        if (eventData.button == PointerEventData.InputButton.Middle)
            if (MouseMiddleClickFunc != null) MouseMiddleClickFunc();


        //Toggle button behaviours:
        if (isClickToggler)
        {
            clickToggle = !clickToggle;

            if (clickToggle)
            {
                if (currentlySelectedButton != null)
                {
                    ClearButtonSelectionCache();
                }

                currentlySelectedButton = this;
            }
            else
            {
                if (currentlySelectedButton == this)
                {
                    ClearButtonSelectionCache();
                }
            }

            ButtonPropertyRefresh(ButtonProperty.Type.ClickToggle);

            if (OnClickToggleFunc != null)
                OnClickToggleFunc();
        }

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isMouseDown = true;
        longPressedExecuted = false;

        //Long press:
        Action onDelay = () =>
        {
            if (isMouseDown)
            {
                isLongPress = true;

                if (OnLongPressFunc != null)
                {
                    OnLongPressFunc();
                }
            }
        };
        StartCoroutine(DelayedAction(onDelay, 1f));

        if (MouseDownOnceFunc != null) MouseDownOnceFunc();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isMouseDown = false;

        //Long press:
        if (isLongPress)
        {
            if (OnLongPressUpFunc != null)
                OnLongPressUpFunc();

            isLongPress = false;

            longPressedExecuted = true;

        }

        if (MouseUpFunc != null) MouseUpFunc();
    }



    //SOUND BEHAVIOURS (Required additional coding):
    public static AudioClip GetSound(Sound sound)
    {
        //Modify this function to add sounds, access them via another script or however your project handles audio.

        /*
        switch (sound)
        {
            case Sound.Positive1:
                return ;
            case Sound.Positive2:
                return ;
            case Sound.Positive3:
                return ;
            case Sound.Positive4:
                return ;
            case Sound.Positive5:
                return ;
            case Sound.Negative1:
                return ;
            case Sound.Negative2:
                return ;
            case Sound.Negative3:
                return;
            case Sound.Negative4:
                return;
            case Sound.Hover1:
                return ;
            case Sound.Hover2:
                return ;
            case Sound.Hover3:
                return ;
            case Sound.Hover4:
                return ;
        }
        */

        return null;
    }

    private void SetSounds()
    {
        if (hoverSound != Sound.Null)
        {
            hoverClip = GetSound(hoverSound);
        }
        if (positiveSound != Sound.Null)
        {
            positiveClip = GetSound(positiveSound);
        }
        if (negativeSound != Sound.Null)
        {
            negativeClip = GetSound(negativeSound);
        }
    }

    public void PlaySelectSound()
    {
        //Add sound script here
    }

    public void PlayNegativeSound()
    {
        //Add sound script here
    }



    //HOVER METHODS:
    public bool IsMouseOver()
    {
        return mouseOver;
    }

    public bool IsPointerOver()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        if (results.Count > 0)
        {
            foreach (RaycastResult result in results)
            {
                if (result.gameObject == this.gameObject)
                    return true;
            }
            return false;
        }
        else
        {
            return false;
        }
        
    }



    //BUTTON PROPERTIES:
    public void ButtonPropertyRefresh(ButtonProperty.Type type)
    {
        //Main refresh for custom button properties:
        foreach (ButtonProperty buttonProperty in buttonPropertyList)
        {
            if (!buttonProperty.isActive)
                continue;

            if (buttonProperty.type == type)
            {
                List<Action> actions = GetButtonPropertyAction(buttonProperty);

                switch (buttonProperty.type)
                {
                    case ButtonProperty.Type.ClickToggle:
                        //buttonProperty.clickToggle = !buttonProperty.clickToggle;
                        if (clickToggle)
                        {
                            if (actions[0] != null)
                            {
                                actions[0]();
                            }
                        }
                        else
                        {
                            if (actions[1] != null)
                            {
                                actions[1]();
                            }
                        }
                        break;
                    case ButtonProperty.Type.Hover:
                        if (mouseOver)
                        {
                            if (actions[0] != null)
                            {
                                actions[0]();
                            }
                        }
                        else
                        {
                            if (actions[1] != null)
                            {
                                actions[1]();
                            }
                        }
                        break;
                }
            }
        }
    }

    public List<Action> GetButtonPropertyAction(ButtonProperty property)
    {
        ButtonProperty.Behaviour behaviour = property.behaviour;

        Action onEnter = null;
        Action onExit = null;
        switch (behaviour)
        {
            case ButtonProperty.Behaviour.ChangeColor:

                onEnter = () => { property.image.color = property.colorIn; };
                onExit = () => { property.image.color = property.colorOut; };

                break;

            case ButtonProperty.Behaviour.ChangeMaterialColor:

                onEnter = () =>
                {
                    Material mat = property.image.material;
                    Material newMat = Instantiate(mat);
                    newMat.SetColor("_Color", property.colorIn);
                    property.image.material = newMat;
                };
                onExit = () =>
                {
                    Material mat = property.image.material;
                    Material newMat = Instantiate(mat);
                    newMat.SetColor("_Color", property.colorOut);
                    property.image.material = newMat;
                };

                break;

            case ButtonProperty.Behaviour.ChangeTextColor:

                onEnter = () => { property.text.color = property.colorIn; };
                onExit = () => { property.text.color = property.colorOut; };

                break;

            case ButtonProperty.Behaviour.ChangeText:

                onEnter = () => { property.text.text = property.textIn; };
                onExit = () => { property.text.text = property.textOut; };

                break;

            case ButtonProperty.Behaviour.ChangeOpacity:

                onEnter = () =>
                {
                    property.canvasGroup.alpha = property.opacityIn;
                };
                onExit = () =>
                {
                    property.canvasGroup.alpha = property.opacityOut;
                };

                break;

            case ButtonProperty.Behaviour.ActivateTooltip:

                onEnter = () =>
                {
                    property.tooltipObject.gameObject.SetActive(true);
                };
                onExit = () =>
                {
                    property.tooltipObject.gameObject.SetActive(false);
                };

                break;

            case ButtonProperty.Behaviour.ObjectTransormation:

                onEnter = () =>
                {
                    if (property.uiRectTransformer.gameObject.activeInHierarchy)
                    {
                        List<TransformPoint> pointList = new List<TransformPoint>() { property.transformPointIn };
                        property.uiRectTransformer.Setup(pointList);
                    } else
                    {
                        property.uiRectTransformer.SetTransformPoint(property.transformPointIn);
                    }
                };

                onExit = () =>
                {
                    if (property.uiRectTransformer.gameObject.activeInHierarchy)
                    {
                        List<TransformPoint> pointList = new List<TransformPoint>() { property.transformPointOut };
                        property.uiRectTransformer.Setup(pointList);
                    }
                    else
                    {
                        property.uiRectTransformer.SetTransformPoint(property.transformPointOut);
                    }
                };

                break;

            case ButtonProperty.Behaviour.ChangeScale:

                onEnter = () =>
                {
                    property.rect.localScale = property.scaleIn;
                };

                onExit = () =>
                {
                    property.rect.localScale = property.scaleOut;
                };

                break;

               
        }

        List<Action> actionList = new List<Action>();
        actionList.Add(onEnter);
        actionList.Add(onExit);
        return actionList;
    }

    public static void ClearButtonSelectionCache()
    {
        //For click togglers:
        if (currentlySelectedButton != null)
        {
            currentlySelectedButton.clickToggle = false;
            currentlySelectedButton.mouseOver = false;

            if (currentlySelectedButton.OnClickToggleFunc != null)
                currentlySelectedButton.OnClickToggleFunc();

            currentlySelectedButton.ButtonPropertyRefresh(ButtonProperty.Type.ClickToggle);
            currentlySelectedButton.ButtonPropertyRefresh(ButtonProperty.Type.Hover);

            currentlySelectedButton = null;
        }
    }

    public void ActivateButtonPropertyBehaviour(ButtonProperty.Behaviour behaviour, bool isActive)
    {
        bool changeOccured = false;

        foreach (ButtonProperty buttonProperty in buttonPropertyList)
        {
            if (buttonProperty.behaviour == behaviour)
            {
                buttonProperty.isActive = isActive;

                changeOccured = true;
            }
        }
    }

    public void ActivateButtonPropertyType(ButtonProperty.Type type, bool isActive)
    {
        bool changeOccured = false;

        foreach (ButtonProperty buttonProperty in buttonPropertyList)
        {
            if (buttonProperty.type == type)
            {
                buttonProperty.isActive = isActive;

                changeOccured = true;
            }
        }
    }



    //EXTERNAL CONTROL METHODS:
    public void SetText(string inText)
    {
        if (text != null)
            text.text = inText;
    }

    public void TextActivate(bool isActive)
    {
        if (text != null)
        {
            text.gameObject.SetActive(isActive);
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (this.mainImage != null)
            this.mainImage.sprite = sprite;
    }

    public void SpriteActivate(bool isActive)
    {
        mainImage.gameObject.SetActive(isActive);
    }

    public void SetSpriteColor(Color color)
    {
        //hoverBehaviour_Image.color
        this.mainImage.color = color;
    }

    public void TooltipActivate(bool isActive)
    {
        tooltipObject.gameObject.SetActive(isActive);
    }

    public void ResetHover()
    {
        mouseOver = false;

        ButtonPropertyRefresh(ButtonProperty.Type.Hover);
    }

    public Vector3 GetTransformPosition()
    {
        return transform.position;
    }

    public void SetAnchoredPosition(Vector2 anchoredPos)
    {
        rect.anchoredPosition = anchoredPos;
    }

    public Vector2 GetAnchoredPosition()
    {
        return rect.anchoredPosition;
    }

    public void SetScaleFactor(float scaleFactor)
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.localScale = new Vector3(1f * scaleFactor, 1f * scaleFactor, 1f * scaleFactor);
    }

    public TextMeshProUGUI GetTextMesh()
    {
        return text;
    }

    public void SetMaterialColor(Color color)
    {
        Material mat = mainImage.material;
        Material newMat = Instantiate(mat);
        newMat.SetColor("_Color", color);
        mainImage.material = newMat;
    }

    public void ResetMaterial()
    {
        mainImage.material = defaultMaterial;
    }



    //COROUTINES:
    private IEnumerator DelayedAction(Action onDelay, float time)
    {
        //Simple method to cause a delayed action

        yield return new WaitForSeconds(time);

        if (onDelay != null)
        {
            onDelay();
        }
    }
}


[Serializable]
public class ButtonProperty
{
    public enum Type
    {
        Hover,
        ClickToggle,
        General,
    }
    [Header("Set Type:")]
    public Type type;

    public enum Behaviour
    {
        ChangeColor,
        ChangeMaterialColor,
        ChangeTextColor,
        ChangeText,
        ChangeOpacity,
        ActivateTooltip,
        ObjectTransormation,
        ChangeScale,
    }
    [Header("Set Behaviour:")]
    public Behaviour behaviour;

    [Header("Changing Image Properties:")]
    public Image image;

    [Header("Changin Color Properties:")]
    public Color colorIn;
    public Color colorOut;

    [Header("Changin Text Properties:")]
    public TextMeshProUGUI text;
    public string textIn;
    public string textOut;

    [Header("Changing Opacity Properties:")]
    public CanvasGroup canvasGroup;
    public float opacityIn;
    public float opacityOut;

    [Header("Chaning Tooltip Properties:")]
    public RectTransform tooltipObject;

    [Header("Changing Scale Properties:")]
    public RectTransform rect;
    public Vector3 scaleIn;
    public Vector3 scaleOut;

    [Header("Changing Object Transformations:")]
    public UI_RectTransformer uiRectTransformer;
    public TransformPoint transformPointIn;
    public TransformPoint transformPointOut;

    [Header("Make Sure this is Active")]
    public bool isActive = true;


    public Action GetHoverActionIn()
    {
        return null;
    }

    public Action GetHoverActionOut()
    {
        return null;
    }
}