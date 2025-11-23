using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace hunt
{


    public class UserCharacter : MonoBehaviour
    {
        private PlayerAction characterAction;
        private GameObject model;

        private bool isSetupComplete = false;
        public bool IsSetupComplete => isSetupComplete;
        private void Start()
        {
            characterAction = GetComponent<PlayerAction>();
            if (characterAction != null)
            {
                characterAction.enabled = false;
            }

            SetUp(HuntKeyConst.Kp_Model_Seible, Vector3.zero).Forget();
        }
        private async UniTask SetUp(string characterKey, Vector3 saveposition)
        {
            try
            {
                if(AbLoader.Shared==null)
                {
                    $"Abloader not set".DError();
                }
                var go = await AbLoader.Shared.LoadAssetAsync<GameObject>(characterKey);
                if (go == null)
                {
                    $"Abloader Error : {characterKey}".DError();
                }
                model = Instantiate<GameObject>(go);
                model.transform.SetParent(transform);
                model.transform.position = Vector3.zero;
                model.transform.rotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                model.transform.position = saveposition;

                if (characterAction != null)
                {
                    characterAction.enabled = true;
                    characterAction.Initialize(model);
                }

                isSetupComplete = true;

            }
            catch(Exception e) 
            {
                $"User Character Setup Fail! {e.Message}".DError();
            }


        }

    }

}