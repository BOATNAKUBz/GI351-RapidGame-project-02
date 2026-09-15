using System;
using System.Collections;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Weapon Slots (0: Shotgun, 1: Revolver, 2: Axe)")]
    public FPSWeapon[] slots = new FPSWeapon[3];
    public int activeSlotIndex = 1; // Default to Revolver

    [Header("Switching Transitions")]
    public float switchDuration = 0.2f;

    [Header("Interaction")]
    public float pickupDistance = 3.0f;
    public LayerMask pickupLayer = ~0;

    public event Action<int, FPSWeapon> OnWeaponSwitched;
    public event Action<FPSWeapon[], int> OnSlotsChanged;

    private bool isSwitching = false;
    private Camera playerCamera;
    private WeaponPickup currentTargetPickup = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        playerCamera = GetComponentInParent<Camera>() ?? Camera.main;
    }

    void Start()
    {
        // Deactivate all weapons except active slot
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].gameObject.SetActive(false);
            }
        }

        // Equip default slot if unlocked, else first unlocked
        if (activeSlotIndex >= 0 && activeSlotIndex < slots.Length && slots[activeSlotIndex] != null && slots[activeSlotIndex].isUnlocked)
        {
            EquipImmediate(activeSlotIndex);
        }
        else
        {
            int firstUnlocked = GetFirstUnlockedSlot();
            if (firstUnlocked >= 0) EquipImmediate(firstUnlocked);
        }

        NotifySlotsChanged();
    }

    void Update()
    {
        HandleWeaponInput();
        CheckPickupTarget();
    }

    void HandleWeaponInput()
    {
        // Slot selection via number keys
        if (InputBridge.GetSlot1Down()) SwitchToSlot(0);
        else if (InputBridge.GetSlot2Down()) SwitchToSlot(1);
        else if (InputBridge.GetSlot3Down()) SwitchToSlot(2);

        // Scroll wheel cycling
        float scroll = InputBridge.GetScrollDelta();
        if (Mathf.Abs(scroll) > 0.01f && !isSwitching)
        {
            int dir = scroll > 0 ? 1 : -1;
            CycleWeapon(dir);
        }

        // Current weapon actions
        FPSWeapon active = GetActiveWeapon();
        if (active != null && !isSwitching)
        {
            if (active.weaponType == WeaponType.Axe || active.isMelee)
            {
                // Battle Axe: ตีฟันระยะใกล้เท่านั้น ยิงไม่ได้เด็ดขาด
                if (InputBridge.GetFireDown() || InputBridge.GetFire())
                {
                    active.TryAttack();
                }
            }
            else
            {
                if (InputBridge.GetFireDown())
                {
                    active.TryAttack();
                }
                if (InputBridge.GetReloadDown())
                {
                    active.TryReload();
                }
            }
        }

        // Pickup interact (E)
        if (InputBridge.GetInteractDown())
        {
            TryPickupTarget();
        }
    }

    void CheckPickupTarget()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        WeaponPickup detected = null;
        if (Physics.Raycast(ray, out hit, pickupDistance, pickupLayer, QueryTriggerInteraction.Collide))
        {
            detected = hit.collider.GetComponentInParent<WeaponPickup>();
        }

        // Sphere overlap fallback if player is close looking near it
        if (detected == null)
        {
            Collider[] cols = Physics.OverlapSphere(playerCamera.transform.position, pickupDistance * 0.8f);
            foreach (var col in cols)
            {
                var p = col.GetComponentInParent<WeaponPickup>();
                if (p != null)
                {
                    detected = p;
                    break;
                }
            }
        }

        if (currentTargetPickup != detected)
        {
            currentTargetPickup = detected;
            if (currentTargetPickup != null)
            {
                GameUIManager.Instance?.ShowInteractPrompt($"Press [E] to pick up {currentTargetPickup.weaponName}");
            }
            else
            {
                GameUIManager.Instance?.HideInteractPrompt();
            }
        }
    }

    void TryPickupTarget()
    {
        if (currentTargetPickup != null)
        {
            WeaponType type = currentTargetPickup.weaponType;
            string wName = currentTargetPickup.weaponName;
            int targetSlot = (int)type;

            // Unlock and equip
            UnlockSlot(targetSlot);
            SwitchToSlot(targetSlot);

            SoundManager.Instance?.PlayWeaponPickup();
            GameUIManager.Instance?.ShowPickupBanner($"Picked up {wName}!");

            currentTargetPickup.Collect();
            currentTargetPickup = null;
            GameUIManager.Instance?.HideInteractPrompt();
        }
    }

    public void UnlockSlot(int slotIdx)
    {
        if (slotIdx >= 0 && slotIdx < slots.Length && slots[slotIdx] != null)
        {
            slots[slotIdx].isUnlocked = true;
            NotifySlotsChanged();
        }
    }

    public void SwitchToSlot(int slotIdx)
    {
        if (slotIdx == activeSlotIndex || slotIdx < 0 || slotIdx >= slots.Length) return;
        if (slots[slotIdx] == null || !slots[slotIdx].isUnlocked) return;
        if (isSwitching) return;

        StartCoroutine(SwitchWeaponRoutine(slotIdx));
    }

    private void CycleWeapon(int direction)
    {
        int count = slots.Length;
        for (int step = 1; step <= count; step++)
        {
            int next = (activeSlotIndex + (direction * step) + count * 2) % count;
            if (slots[next] != null && slots[next].isUnlocked)
            {
                SwitchToSlot(next);
                return;
            }
        }
    }

    private IEnumerator SwitchWeaponRoutine(int targetSlot)
    {
        isSwitching = true;
        FPSWeapon current = GetActiveWeapon();

        // Holster current weapon (move down)
        if (current != null && current.gameObject.activeSelf)
        {
            Transform tr = current.gunTransform != null ? current.gunTransform : current.transform;
            Vector3 startPos = tr.localPosition;
            Vector3 lowerPos = startPos - new Vector3(0, 0.35f, 0.1f);

            float t = 0f;
            while (t < switchDuration)
            {
                t += Time.deltaTime;
                tr.localPosition = Vector3.Lerp(startPos, lowerPos, t / switchDuration);
                yield return null;
            }

            current.gameObject.SetActive(false);
            tr.localPosition = startPos;
        }

        // Activate new weapon
        activeSlotIndex = targetSlot;
        FPSWeapon next = slots[activeSlotIndex];
        if (next != null)
        {
            next.gameObject.SetActive(true);
            SoundManager.Instance?.PlayWeaponPickup();

            Transform tr = next.gunTransform != null ? next.gunTransform : next.transform;
            Vector3 targetPos = tr.localPosition;
            Vector3 lowerPos = targetPos - new Vector3(0, 0.35f, 0.1f);
            tr.localPosition = lowerPos;

            float t = 0f;
            while (t < switchDuration)
            {
                t += Time.deltaTime;
                tr.localPosition = Vector3.Lerp(lowerPos, targetPos, t / switchDuration);
                yield return null;
            }
            tr.localPosition = targetPos;
        }

        isSwitching = false;
        OnWeaponSwitched?.Invoke(activeSlotIndex, next);
        NotifySlotsChanged();
    }

    private void EquipImmediate(int slotIdx)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) slots[i].gameObject.SetActive(i == slotIdx);
        }
        activeSlotIndex = slotIdx;
        OnWeaponSwitched?.Invoke(activeSlotIndex, slots[activeSlotIndex]);
    }

    public FPSWeapon GetActiveWeapon()
    {
        if (activeSlotIndex >= 0 && activeSlotIndex < slots.Length)
        {
            return slots[activeSlotIndex];
        }
        return null;
    }

    public int GetFirstUnlockedSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].isUnlocked) return i;
        }
        return -1;
    }

    public void NotifySlotsChanged()
    {
        OnSlotsChanged?.Invoke(slots, activeSlotIndex);
        var active = GetActiveWeapon();
        if (active != null)
        {
            active.NotifyAmmo();
        }
    }
}
