using System.Collections;
using TMPro;
using UnityEngine;

public class TextController : MonoBehaviour {
	private enum StageRequirement {
		Wasd,
		LeftClick,
		TVTurnOn
	}

	[SerializeField] private TMP_Text tutorialText;
	[SerializeField] private TMP_Text tutorial1Text;
	[SerializeField] private TMP_Text tutorial2Text;
	[SerializeField] private GameObject TVTurnOnText;
	[SerializeField] private float stageHideDelay = 2f;
	[SerializeField] private GameObject RemotoController;
	[SerializeField] private Camera plaercamera;
	[SerializeField] private float interactionDistance = 3f;
	[SerializeField] private LayerMask interactionLayerMask = ~0;

	private TMP_Text[] tutorialStages;
	private StageRequirement[] stageRequirements;
	private int currentStageIndex;
	private bool pressedW;
	private bool pressedA;
	private bool pressedS;
	private bool pressedD;
	private bool pressedLeftClick;
	private bool IsTVTurnOn;
	private bool transitionScheduled;
	private string originalText;

	private void Awake() {
		if (tutorialText == null) {
			tutorialText = GetComponent<TMP_Text>();
		}
	}

	private void Start() {
		if (tutorialText == null) {
			return;
		}

		if (plaercamera == null) {
			plaercamera = Camera.main;
		}

		tutorialStages = new TMP_Text[3];
		tutorialStages[0] = tutorialText;
		tutorialStages[1] = tutorial1Text;
		tutorialStages[2] = tutorial2Text;

		if (tutorialStages[1] == null) {
			tutorialStages[1] = CreateStageClone("Tutorial (1)");
		}

		if (tutorialStages[2] == null) {
			tutorialStages[2] = CreateStageClone("Tutorial (2)");
		}

		stageRequirements = new StageRequirement[] {
			StageRequirement.Wasd,
			StageRequirement.LeftClick,
			StageRequirement.TVTurnOn,
		};

		if (tutorialStages[1] != null) {
			tutorialStages[1].gameObject.SetActive(false);
		}

		if (tutorialStages[2] != null) {
			tutorialStages[2].gameObject.SetActive(false);
		}

		if (TVTurnOnText != null) {
			TVTurnOnText.SetActive(false);
		}

		ActivateStage(0);
	}

	private TMP_Text FindStageText(string stageName) {
		TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
		for (int i = 0; i < allTexts.Length; i++) {
			TMP_Text text = allTexts[i];
			if (text != null && text.gameObject.name == stageName) {
				return text;
			}
		}

		return null;
	}

	private void Update() {
		if (tutorialText == null || !tutorialText.enabled || transitionScheduled) {
			return;
		}

		bool changed = false;

		if (stageRequirements != null && currentStageIndex >= 0 && currentStageIndex < stageRequirements.Length) {
			if (stageRequirements[currentStageIndex] == StageRequirement.Wasd) {
				if (!pressedW && Input.GetKeyDown(KeyCode.W)) {
					pressedW = true;
					changed = true;
				}
				if (!pressedA && Input.GetKeyDown(KeyCode.A)) {
					pressedA = true;
					changed = true;
				}
				if (!pressedS && Input.GetKeyDown(KeyCode.S)) {
					pressedS = true;
					changed = true;
				}
				if (!pressedD && Input.GetKeyDown(KeyCode.D)) {
					pressedD = true;
					changed = true;
				}
			}
			else if (stageRequirements[currentStageIndex] == StageRequirement.LeftClick && !pressedLeftClick && Input.GetMouseButtonDown(0)) {
				pressedLeftClick = true;
				changed = true;
			}
			else if (stageRequirements[currentStageIndex] == StageRequirement.TVTurnOn) {
				bool lookingAtRemoto = IsLookingAtRemotoController();

				if (TVTurnOnText != null && TVTurnOnText.activeSelf != lookingAtRemoto) {
					TVTurnOnText.SetActive(lookingAtRemoto);
				}

				if (lookingAtRemoto && Input.GetKeyDown(KeyCode.E)) {
					TVTurnOn();
				}
			}
		}

		if (changed) {
			RefreshText();
		}
	}

	private bool IsLookingAtRemotoController() {
		if (RemotoController == null) {
			return false;
		}

		Camera cam = plaercamera != null ? plaercamera : Camera.main;
		if (cam == null) {
			return false;
		}

		Ray ray = new Ray(cam.transform.position, cam.transform.forward);
		if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayerMask)) {
			return hit.transform == RemotoController.transform || hit.transform.IsChildOf(RemotoController.transform);
		}

		return false;
	}

	private TMP_Text CreateStageClone(string stageName) {
		GameObject cloneObject = Instantiate(tutorialText.gameObject, tutorialText.transform.parent);
		cloneObject.name = stageName;
		cloneObject.SetActive(false);

		TextController clonedController = cloneObject.GetComponent<TextController>();
		if (clonedController != null) {
			Destroy(clonedController);
		}

		TMP_Text clonedText = cloneObject.GetComponent<TMP_Text>();

		return clonedText;
	}

	private void ActivateStage(int stageIndex) {
		if (tutorialStages == null || stageIndex < 0 || stageIndex >= tutorialStages.Length) {
			return;
		}

		currentStageIndex = stageIndex;
		transitionScheduled = false;
		ResetKeyState();
		HideOtherStages(stageIndex);
		tutorialText = tutorialStages[currentStageIndex];

		if (TVTurnOnText != null) {
			TVTurnOnText.SetActive(false);
		}

		if (tutorialText == null) {
			return;
		}

		tutorialText.gameObject.SetActive(true);
		tutorialText.enabled = true;
		originalText = GetStageOriginalText(currentStageIndex, tutorialText.text);
		tutorialText.text = originalText;
		RefreshText();
	}

	private void HideOtherStages(int visibleStageIndex) {
		if (tutorialStages == null) {
			return;
		}

		for (int i = 0; i < tutorialStages.Length; i++) {
			TMP_Text stageText = tutorialStages[i];
			if (stageText == null) {
				continue;
			}

			if (i == visibleStageIndex) {
				continue;
			}

			stageText.enabled = false;
			stageText.gameObject.SetActive(false);
		}
	}

	private void RefreshText() {
		if (tutorialText == null) {
			return;
		}

		if (IsStageComplete()) {
			tutorialText.text = BuildColoredText();
			if (!transitionScheduled) {
				transitionScheduled = true;
				StartCoroutine(AdvanceAfterDelay());
			}
			return;
		}

		if (string.IsNullOrEmpty(originalText)) {
			originalText = tutorialText.text;
		}

		tutorialText.text = BuildColoredText();
	}

	private string GetStageOriginalText(int stageIndex, string fallbackText) {
		return fallbackText;
	}

	private bool IsStageComplete() {
		if (stageRequirements == null || currentStageIndex < 0 || currentStageIndex >= stageRequirements.Length) {
			return false;
		}

		if (stageRequirements[currentStageIndex] == StageRequirement.LeftClick) {
			return pressedLeftClick;
		}

		if (stageRequirements[currentStageIndex] == StageRequirement.TVTurnOn) {
			return IsTVTurnOn;
		}

		return pressedW && pressedA && pressedS && pressedD;
	}

	private IEnumerator AdvanceAfterDelay() {
		yield return new WaitForSeconds(stageHideDelay);

		if (tutorialStages == null || currentStageIndex < 0 || currentStageIndex >= tutorialStages.Length) {
			yield break;
		}

		TMP_Text currentText = tutorialStages[currentStageIndex];
		if (currentText != null) {
			currentText.enabled = false;
		}

		int nextStageIndex = currentStageIndex + 1;
		if (nextStageIndex < tutorialStages.Length && tutorialStages[nextStageIndex] != null) {
			ActivateStage(nextStageIndex);
		}
	}

	private void ResetKeyState() {
		pressedW = false;
		pressedA = false;
		pressedS = false;
		pressedD = false;
		pressedLeftClick = false;
		IsTVTurnOn = false;
	}

	private string BuildColoredText() {
		if (string.IsNullOrEmpty(originalText)) {
			return string.Empty;
		}

		if (stageRequirements != null && currentStageIndex >= 0 && currentStageIndex < stageRequirements.Length) {
			if (stageRequirements[currentStageIndex] == StageRequirement.LeftClick) {
				return pressedLeftClick ? originalText.Replace("左クリック", "<color=red>左クリック</color>") : originalText;
			}
		}

		return originalText
			.Replace("W", pressedW ? "<color=red>W</color>" : "W")
			.Replace("A", pressedA ? "<color=red>A</color>" : "A")
			.Replace("S", pressedS ? "<color=red>S</color>" : "S")
			.Replace("D", pressedD ? "<color=red>D</color>" : "D");
	}

	private void TVTurnOn() {
		if (currentStageIndex == 2 && !IsTVTurnOn) {
			IsTVTurnOn = true;
			if (TVTurnOnText != null) {
				TVTurnOnText.SetActive(true);
			}
		}
		RefreshText();
	}
}