using System.Collections.Generic;
using MirralLogger.Runtime.Core;
using MirralLogger.Runtime.Model;
using UnityEngine;
using UnityEngine.Serialization;

public class SpiderLegIK : MonoBehaviour
{
    [Header(" <0~n> 自根部到末端")] public List<Transform> joints = new();

    [Header("目标点")] public Transform IKTarget;

    [SerializeField] private Vector3 origin;
    [SerializeField] private float[] distances;

    [FormerlySerializedAs("oldPos")] [SerializeField]
    private Vector3[] pos;

    private void Start()
    {
        if (joints.Count == 0)
        {
            MLogger.Log("未设置节点！", LogLevel.Warning, LogCategory.Animation, this);
        }

        distances = new float[joints.Count - 1];
        pos = new Vector3[joints.Count];

        //记录根节点
        origin = joints[0].transform.position;

        //计算每一段固定距离
        for (int i = 0; i < distances.Length; i++)
        {
            distances[i] = (joints[i + 1].position - joints[i].position).magnitude;
            pos[i] = joints[i].position;
        }
    }

    private void LateUpdate()
    {
        IK();
        ApplyPos();
        ApplyRot();
    }

    private void IK()
    {
        origin = joints[0].transform.position;

        for (int time = 0; time < 5; time++)
        {
            pos[^1] = IKTarget.position;

            //1. 沿着原链条方向，控制每个节点之间距离一致，计算每个点的新位置
            for (int i = joints.Count - 1; i > 0; i--)
            {
                //尾指向头
                var direction = Vector3.Normalize(pos[i - 1] - pos[i]);
                //算出每一个关节的位置
                pos[i - 1] = pos[i] + direction * distances[i - 1];
            }

            //2. 把初始的节点恢复到初始位置
            pos[0] = origin;

            for (int i = 0; i < pos.Length - 1; i++)
            {
                var direction = Vector3.Normalize(pos[i + 1] - pos[i]);
                pos[i + 1] = pos[i] + direction * distances[i];
            }
        }
    }

    private void ApplyPos()
    {
        for (int i = 0; i < joints.Count; i++)
        {
            joints[i].position = pos[i];
        }
    }

    private void ApplyRot()
    {
        for (int i = 0; i < joints.Count - 1; i++)
        {
            joints[i].rotation = Quaternion.LookRotation(joints[i + 1].position - joints[i].position);
        }
    }
}