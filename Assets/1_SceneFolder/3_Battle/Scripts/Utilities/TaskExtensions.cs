// Assets/Battle/Scripts/Utilities/TaskExtensions.cs
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// Task를 코루틴으로 변환하는 확장 메서드.
/// </summary>
public static class TaskExtensions
{
    public static IEnumerator AsCoroutine(this Task task)
    {
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) throw task.Exception;
    }
}