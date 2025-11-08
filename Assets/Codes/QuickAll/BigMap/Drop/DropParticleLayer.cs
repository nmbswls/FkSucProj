using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DropParticleLayer : MonoBehaviour
{
    private ParticleSystem _ps;
    private ParticleSystem.TextureSheetAnimationModule _tsa;
    private Sprite[] _sprites; // TSA中已添加的帧列表缓存
    private ParticleSystem.Particle[] _buffer;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        _tsa = _ps.textureSheetAnimation;

        // 读取 TSA 面板里添加的帧（Unity 2021+ 支持 GetSprite）
        int count = _tsa.spriteCount;
        _sprites = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            _sprites[i] = _tsa.GetSprite(i);
        }

        // 根据最大粒子数准备缓存
        var main = _ps.main;
        int max = main.maxParticles;
        _buffer = new ParticleSystem.Particle[Mathf.Max(1024, max)];
    }

    public Sprite TryGetFirstSpriteFromTSA()
    {
        var tsa = _ps.textureSheetAnimation;
        if (tsa.enabled && tsa.mode == ParticleSystemAnimationMode.Sprites && tsa.spriteCount > 0)
        {
            return tsa.GetSprite(0);
        }
        return null;
    }


    // 根据索引发射一个代表掉落物的粒子
    // spriteIndex: 对应 TSA 列表中的帧索引（0..spriteCount-1）
    public int EmitDrop(Vector3 pos, int spriteIndex, float size, Color color, float lifetime = 99999f)
    {
        if (spriteIndex < 0 || spriteIndex >= _sprites.Length)
        {
            Debug.LogError($"EmitDrop spriteIndex out of range: {spriteIndex}");
            spriteIndex = Mathf.Clamp(spriteIndex, 0, _sprites.Length - 1);
        }

        var emit = new ParticleSystem.EmitParams
        {
            position = pos,
            startSize = size,
            startColor = color,
            startLifetime = lifetime,
            //sprite = _sprites[spriteIndex] // 关键：指定该粒子的图集帧
        };

        _ps.Emit(emit, 1);

        // 取出当前粒子总数并返回新粒子的索引（简单近似：最后一个）
        int count = _ps.GetParticles(_buffer);
        int idx = count - 1;
        return Mathf.Max(idx, 0);
    }

    // 删除/隐藏某个粒子
    public void KillParticle(int particleIndex)
    {
        int count = _ps.GetParticles(_buffer);
        if (particleIndex >= 0 && particleIndex < count)
        {
            _buffer[particleIndex].remainingLifetime = 0f;
            _ps.SetParticles(_buffer, count);
        }
    }

    // 可选：批量更新（例如改变颜色/大小），尽量减少SetParticles调用频率
    public void SetParticleColor(int particleIndex, Color color)
    {
        int count = _ps.GetParticles(_buffer);
        if (particleIndex >= 0 && particleIndex < count)
        {
            _buffer[particleIndex].startColor = color;
            _ps.SetParticles(_buffer, count);
        }
    }
}