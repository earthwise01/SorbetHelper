using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;

namespace Celeste.Mod.SorbetHelper.Utils {
    public static class Extensions {
        public static bool GetFlag(this Session session, string flag, bool inverted) =>
            session.GetFlag(flag) != inverted;

        // ParticleSystem.Emit directionRange stuff
        public static void Emit(this ParticleSystem self, ParticleType type, int amount, Vector2 position, float direction, float directionRange) {
            for (int i = 0; i < amount; i++) {
                self.Emit(type, position, Calc.Random.Range(direction - directionRange, direction + directionRange));
            }
        }
        public static void Emit(this ParticleSystem self, ParticleType type, int amount, Vector2 position, Color color, float direction, float directionRange) {
            for (int i = 0; i < amount; i++) {
                self.Emit(type, position, color, Calc.Random.Range(direction - directionRange, direction + directionRange));
            }
        }
        public static void Emit(this ParticleSystem self, ParticleType type, int amount, Vector2 position, Vector2 positionRange, float direction, float directionRange) {
            for (int i = 0; i < amount; i++) {
                self.Emit(type, Calc.Random.Range(position - positionRange, position + positionRange), Calc.Random.Range(direction - directionRange, direction + directionRange));
            }
        }
        public static void Emit(this ParticleSystem self, ParticleType type, int amount, Vector2 position, Vector2 positionRange, Color color, float direction, float directionRange) {
            for (int i = 0; i < amount; i++) {
                self.Emit(type, Calc.Random.Range(position - positionRange, position + positionRange), color, Calc.Random.Range(direction - directionRange, direction + directionRange));
            }
        }
    }
}
