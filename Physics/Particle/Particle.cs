using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Physics
{
    public class Particle
    {
        static int GUID = 0;

        protected Vertex3f _position = Vertex3f.Zero;
        protected Vertex3f _velocity = Vertex3f.Zero;
        protected Vertex3f _acceleration = Vertex3f.Zero;
        protected Vertex3f _forceAccum = Vertex3f.Zero;
        protected Vertex3f _color = Vertex3f.Zero;
        protected float _inverseMass = 1.0f;
        protected float _damping = 0.99f;
        protected float _life = float.MaxValue;
        int _guid;
        bool _isRegisty = false;
        float _halfSize = 0.1f;

        public float HalfSize
        {
            get => _halfSize;
            set => _halfSize = value;
        }

        public bool IsRegisty
        {
            get => _isRegisty; 
            set => _isRegisty = value;
        }

        protected List<int> _forceRegistrations;

        public List<int> Forces => _forceRegistrations;

        public int Guid => _guid;

        public float Life
        {
            get => _life; 
            set => _life = value;
        }

        public Vertex3f Color
        {
            get => _color;
            set => _color = value;
        }

        public Vertex3f Position
        {
            get => _position; 
            set => _position = value;
        }

        public Vertex3f Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        public Vertex3f Acceleration
        {
            get => _acceleration;
            set => _acceleration = value;
        }

        public float Damping
        {
            get => _damping;
            set => _damping = value;
        }

        public float InverseMass
        {
            get => _inverseMass;
            set => _inverseMass = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public float Mass
        {
            get => _inverseMass <= 0.0000001f ? float.MaxValue : 1.0f / _inverseMass;
            set => _inverseMass = (value > 0.0000001f) ? 1.0f / value : float.MaxValue;
        }

        public bool HasFiniteMass => _inverseMass >= 0.0f;

        public Particle()
        {
            GUID++;
            _guid = GUID;
            _color = Rand.NextColor3f;
            _forceRegistrations = new List<int>();
        }

        public void Integrate(float duration)
        {
            // We don't integrate things with zero mass.
            if (_inverseMass <= 0.0f) return;
            BoolF.Assert(duration > 0.0f);

            // Update linear position.
            _position += _velocity * duration;

            // Work out the acceleration from the force
            Vertex3f resultingAcc = _acceleration;
            resultingAcc += _forceAccum * _inverseMass;

            // Update linear velocity from the acceleration.
            _velocity += resultingAcc * duration;

            // Impose drag.
            _velocity *= Math.Pow(_damping, duration);

            // Clear the forces.
            ClearAccumulator();
        }

        public void ClearAccumulator()
        {
            _forceAccum = Vertex3f.Zero;
        }

        public void AddForce(Vertex3f force)
        {
            _forceAccum += force;
        }

        public void AddForceRegistration(int forceRegistration)
        {
            _forceRegistrations.Add(forceRegistration);
        }

    }
}
