using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Character : MonoBehaviour, ISaveable
{
    [Header("事件監聽")]
    public VoidEventSO newGameEvent;
    [Header("基本屬性")]
    public float maxHealth;
    public float currentHealth;

    [Header("受傷無敵")]
    public float invulnerableDuration;//無敵時間
    private float invulnerableCounter;//無敵計時器
    public bool invulnerable;//無敵狀態

    public UnityEvent<Character> OnHealthChange;
    public UnityEvent<Transform> OnTakeDamage;
    public UnityEvent OnDie;

    private void Start()
    {
        currentHealth = maxHealth;
    }
    private void NewGame()
    {
        currentHealth = maxHealth;
        OnHealthChange?.Invoke(this);
    }
    private void OnEnable()
    {
        newGameEvent.OnEventRaised += NewGame;
        ISaveable saveable = this;
        saveable.RegisterSaveData();
    }
    private void OnDisable()
    {
        newGameEvent.OnEventRaised -= NewGame;
        ISaveable saveable = this;
        saveable.UnRegisterSaveData();
    }

    private void Update()
    {
        if (invulnerable)
        {
            invulnerableCounter -= Time.deltaTime;
            if (invulnerableCounter <= 0)
            {
                invulnerable = false;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            //死亡，更新血量
            currentHealth = 0;
            OnHealthChange?.Invoke(this);
            OnDie?.Invoke();
        }
    }
    public void TakeDamage(Attack attacker)
    {
        if (invulnerable)
        {
            return;
        }
        //Debug.Log(attacker.damage);
        if (currentHealth - attacker.damage > 0)
        {
            currentHealth -= attacker.damage;
            TriggerInvulnerable();
            //執行受傷
            OnTakeDamage?.Invoke(attacker.transform);
        }
        else
        {
            currentHealth = 0;
            //觸發死亡
            OnDie?.Invoke();
        }

        OnHealthChange?.Invoke(this);
    }
    /// <summary>
    /// 觸發受傷無敵
    /// </summary>
    private void TriggerInvulnerable()
    {
        if (!invulnerable)
        {
            invulnerable = true;
            invulnerableCounter = invulnerableDuration;
        }
    }

    public DataDefinition GetDataID()
    {
        return GetComponent<DataDefinition>();
    }

    public void GetSaveData(Data data)
    {
        if (data.characterPosDict.ContainsKey(GetDataID().ID))
        {
            data.characterPosDict[GetDataID().ID] = transform.position;
            data.floatSavedData[GetDataID().ID + "health"] = this.currentHealth;
        }
        else
        {
            data.characterPosDict.Add(GetDataID().ID, transform.position);
            data.floatSavedData.Add(GetDataID().ID + "health", this.currentHealth);
        }
    }

    public void LoadData(Data data)
    {
        if (data.characterPosDict.ContainsKey(GetDataID().ID))
        {
            transform.position = data.characterPosDict[GetDataID().ID];
            this.currentHealth = data.floatSavedData[GetDataID().ID + "health"];

            //通知UI更新
            OnHealthChange?.Invoke(this);
        }
    }
}
