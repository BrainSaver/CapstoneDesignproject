using System.Collections;

/// <summary>
/// 코루틴 기반으로 동작하는 이펙트 인터페이스.
/// 애니메이션·딜레이가 필요한 이펙트에 사용한다.
/// </summary>
public interface ICoroutineEffect
{
    IEnumerator ApplyEffectRoutine(CharacterStats source, CharacterStats target);
}