using System;
using System.Collections;
using System.Collections.Generic;
using MirralLogger.Runtime.Core;
using MirralLogger.Runtime.Model;
using Unity.Collections;
using UnityEngine;

public class SpiderLegIK : MonoBehaviour
{
    [Header(" <0~n> 自根部到末端")] public List<Transform> joints = new();

    public List<float> distances = new();

    [Header("目标点")] public Transform IK_Target;
    
    [SerializeField] private Transform root;
    [SerializeField] private int jointCount;

    private void Start()
    {
        //记录节点数
        jointCount = joints.Count;

        if (jointCount == 0)
        {
            MLogger.Log("未设置节点！", LogLevel.Warning, LogCategory.Animation, this);
        }

        //记录根节点
        root = joints[0].transform;

        for (int i = 0; i < jointCount - 1; i++)
        {
            distances.Add((joints[i + 1].position - joints[i].position).magnitude);
        }
    }

    private void LateUpdate()
    {
        IK();
    }

    private void IK()
    {
        FrontIK();
    }

    private void FrontIK()
    {
        //1. 末端节点等于目标位置
        joints[jointCount - 1].position = IK_Target.position;

        //2. 沿着原链条方向，控制每个节点之间距离一致
        for (int i = jointCount - 1; i > 0; i--)
        {
            //将一段分为头尾两个节点

            //尾
            var tail = joints[i].position;
            //头
            var head = joints[i - 1].position;

            //原来头到尾的距离
            var length = distances[i - 1];

            //尾指向头的方向
            var direction = Vector3.Normalize(head - tail);
            //头位置
            var target = direction * distances[i - 1];

            //现在头的位置
            joints[i - 1].position = target;
        }
    }

    private void BackIK()
    {
        //把根节点恢复到根位置
        joints[0].position = root.position;

        for (int i = 0; i < jointCount - 1; i++)
        {
            //头位置
            var head = joints[i].position;
            //尾位置
            var tail = joints[i + 1].position;
            
            var length = distances[i];
            
            //现在头到尾的方向
            var direction = Vector3.Normalize(tail - head);
            
            joints[i+1].position = direction * distances[i];
        }
    }
}