using System.Collections.Generic;

namespace GameSystems.Abilities
{
    public sealed class CharacterRequestBuffer
    {
        readonly List<AbilityRequest> requests = new(16);
        public IReadOnlyList<AbilityRequest> Requests => requests;

        public void Add(in AbilityRequest request)
        {
            if (request.Ability != null) requests.Add(request);
        }

        public void Clear() => requests.Clear();
    }
}
