using System.Collections.Generic;
using System.Linq;
using MirralLogger.Runtime.Core;
using MirralLogger.Runtime.Model;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;

public class SpiderLegIK : MonoBehaviour
{
    [Header(" <0~n> 自根部到末端")] public List<Transform> joints = new();

    [Header("目标点")] public Transform IKTarget;

    [SerializeField] private Vector3 origin;
    [SerializeField] private float[] distances;

    //旋转相关
    [SerializeField] private Vector3[] localForwards;
    [SerializeField] private Vector3[] localUps;
    
    [FormerlySerializedAs("oldPos")] [SerializeField]
    private Vector3[] pos;
    
    private bool initialized = false;

    private void Start()
    {
        if (joints.Count == 0)
        {
            MLogger.Log("未设置节点！", LogLevel.Warning, LogCategory.Animation, this);
            return;
        }

        if (!IKTarget)
        {
            MLogger.Log("没有设置IK 目标对象",LogLevel.Error,LogCategory.Animation,this);
            return;
        }
        
        distances = new float[joints.Count - 1];
        pos = new Vector3[joints.Count];
        
        localForwards = new Vector3[joints.Count];
        localUps = new Vector3[joints.Count];
        
        //记录根节点
        origin = joints[0].transform.position;

        //计算每一段固定距离
        for (int i = 0; i < distances.Length; i++)
        {
            var jointVector = joints[i + 1].position - joints[i].position;
            distances[i] = jointVector.magnitude;
            pos[i] = joints[i].position;
            
            //记录初始旋转,将每一小节的世界坐标向量转到父关节的本地坐标空间，作为参考前向量
            localForwards[i] = joints[i].InverseTransformDirection(jointVector.normalized);
            localUps[i] = joints[i].InverseTransformDirection(joints[i].up);
        }
        pos[^1] = joints[^1].position;
        
        initialized = true;
    }

    private void LateUpdate()
    {
        IK();
        ApplyPos();
        ApplyRot();
    }

    //迭代解算每个节点应该在哪
    private void IK()
    {
        if (!initialized)  return;
        
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
            //当前应该指向的世界方向
            var targetDir = Vector3.Normalize(pos[i + 1] - pos[i]);
            
            //拿到目前的本地forward在世界坐标的方向
            var currentForward = joints[i].TransformDirection(localForwards[i]);
            
            //旋转差
            var delta = Quaternion.FromToRotation(currentForward, targetDir);
            
            //增加极向量，使得所有关节不会乱拧
            joints[i].rotation = Quaternion.LookRotation(
                targetDir,
                joints[i].TransformDirection(localUps[i])
            );
            
            joints[i].rotation = delta * joints[i].rotation;
        }
    }
}