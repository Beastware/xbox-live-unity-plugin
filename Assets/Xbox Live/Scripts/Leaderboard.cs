﻿using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Xbox.Services.Leaderboard;

using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

public class Leaderboard : MonoBehaviour
{
    
    public string leaderboardName;

    [Range(1, 100)]
    public uint entryCount = 10;

    public uint currentPage = 0;

    public uint totalPages;

    private TaskYieldInstruction<LeaderboardResult> leaderboardData;
    
    public Text headerText;
    public Text pageText;

    public Button firstButton;
    public Button previousButton;
    public Button nextButton;
    public Button lastButton;

    public Transform contentPanel;

    private ObjectPool entryObjectPool;

    public void Awake()
    {
        this.entryObjectPool = this.GetComponent<ObjectPool>();
        this.UpdateButtons();
    }

    public void Refresh()
    {
        this.FirstPage();
    }

    public void NextPage()
    {
        this.currentPage++;
        this.UpdateData(this.leaderboardData.Result.GetNextAsync(this.entryCount));
    }

    public void PreviousPage()
    {
        this.currentPage--;
        this.UpdateData(XboxLive.Instance.Context.LeaderboardService.GetLeaderboardAsync(
            XboxLive.Instance.Configuration.PrimaryServiceConfigId,
            this.leaderboardName,
            this.currentPage * this.entryCount,
            this.entryCount));
    }

    public void FirstPage()
    {
        this.currentPage = 0;
        this.UpdateData(XboxLive.Instance.Context.LeaderboardService.GetLeaderboardAsync(
            XboxLive.Instance.Configuration.PrimaryServiceConfigId,
            this.leaderboardName,
            this.currentPage * this.entryCount,
            this.entryCount));
    }

    public void LastPage()
    {
        this.currentPage = this.totalPages - 1;
        this.UpdateData(XboxLive.Instance.Context.LeaderboardService.GetLeaderboardAsync(
            XboxLive.Instance.Configuration.PrimaryServiceConfigId,
            this.leaderboardName,
            this.currentPage * this.entryCount,
            this.entryCount));
    }

    private void UpdateData(Task<LeaderboardResult> task)
    {
        this.StartCoroutine(this.UpdateData(task.AsCoroutine()));
    }

    private IEnumerator UpdateData(TaskYieldInstruction<LeaderboardResult> data)
    {
        this.leaderboardData = data;
        yield return this.leaderboardData;

        if (this.totalPages == 0)
        {
            // This is the first update we're doing.  Setup some initial properties.
            this.headerText.text = this.leaderboardData.Result.DisplayName;
            this.totalPages = (this.leaderboardData.Result.TotalRowCount - 1) / this.entryCount + 1;
        }

        this.pageText.text = string.Format("Page: {0} / {1}", this.currentPage + 1, this.totalPages);

        while (this.contentPanel.childCount > 0)
        {
            var entry = this.contentPanel.GetChild(0).gameObject;
            this.entryObjectPool.ReturnObject(entry);
        }

        foreach (LeaderboardRow row in this.leaderboardData.Result.Rows)
        {
            GameObject entryObject = this.entryObjectPool.GetObject();
            LeaderboardEntry entry = entryObject.GetComponent<LeaderboardEntry>();
            
            entry.Data = row;

            entryObject.transform.SetParent(this.contentPanel);
        }

        this.UpdateButtons();
    }

    public void UpdateButtons()
    {
        this.firstButton.interactable = this.previousButton.interactable = XboxLive.IsEnabled && this.currentPage != 0;
        this.nextButton.interactable = this.lastButton.interactable = XboxLive.IsEnabled && this.totalPages > 1 && this.currentPage < this.totalPages - 1;
    }
}