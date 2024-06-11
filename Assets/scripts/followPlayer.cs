using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class followPlayer : NetworkBehaviour
{

    Cinemachine.CinemachineTargetGroup.Target target;
    public CinemachineVirtualCamera vcam;

    private CinemachineTargetGroup targetGroup;
    private GameObject[] players;
    private int playersConnected;


    private void Start()
    {
        targetGroup = GameObject.Find("Target Group").GetComponent<CinemachineTargetGroup>();
        players = GameObject.FindGameObjectsWithTag("Player");
        playersConnected = players.Length;
        target.weight = 1;
        target.radius = 2;

        for (int i = 0; i < players.Length; i++)
        {
            target.target = players[i].GetComponent<Transform>();

            targetGroup.AddMember(target.target, target.weight, target.radius);
            Debug.Log("added player");
        }
    }

    public void Update()    
    {
        if(playersConnected != GameObject.FindGameObjectsWithTag("Player").Length) {
            players = GameObject.FindGameObjectsWithTag("Player");
            playersConnected = players.Length;
            Debug.Log(playersConnected);
            targetGroup.AddMember(players[players.Length-1].GetComponent<Transform>(),
                                   target.weight, 
                                   target.radius);
            Debug.Log("new player added to targetGroup");
        }
    }
}

//vcam.Follow = GameObject.FindGameObjectsWithTag("Player").transform;

//for (int i = 0; i < targetGroup.m_Targets.Length; i++)
//{
//    if (targetGroup.m_Targets[i].target == null)
//    {
//        targetGroup.m_Targets.SetValue(target, i);
//        return;
//    }
//}