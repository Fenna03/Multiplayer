using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ButtonScript : NetworkBehaviour
{
    public Animator anim;
    public fanOnOff fanScript;
    public fireOnOff fireScript;
    public trampolineOnOff trampolineScript;

    //public BoxCollider2D BC;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnButtonPressServerRpc(ServerRpcParams serverRpcParams = default)
    {
        PressClientRpc();
    }

    [ClientRpc]
    void PressClientRpc()
    {
        anim.SetBool("isPressed", true);
        anim.SetBool("isReleased", false);
        if(fanScript != null)
        {
            fanScript.On();
        }
        if (fireScript != null)
        {
            fireScript.Off();
        }
        if (trampolineScript != null)
        {
            trampolineScript.On();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnButtonReleaseServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ReleaseClientRpc();
    }

    [ClientRpc]
    void ReleaseClientRpc()
    {
        //Debug.Log("test");
        anim.SetBool("isPressed", false);
        anim.SetBool("isReleased", true);
        if (fanScript != null)
        {
            Debug.Log("fan test");
            fanScript.Off();
        }
        if (fireScript != null)
        {
            Debug.Log("fire test");
            fireScript.On();
        }
        if (trampolineScript != null)
        {
            Debug.Log("trampoline test");
            trampolineScript.Off();
        }
    }
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
           // Debug.Log("player touches");
            OnButtonPressServerRpc();
        }
    }
    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
           // Debug.Log("player stop touches");
            OnButtonReleaseServerRpc();
        }
    }
}
