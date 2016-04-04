﻿using System.Collections.Generic;
using System.Globalization;
using System.Yaml;
using CETech.Develop;
using MsgPack;

namespace CETech.World
{
    public partial class TranformationSystem
    {
        private static readonly Dictionary<int, WorldInstance> _worldInstance = new Dictionary<int, WorldInstance>();

        private static int getIdx(int world, int entity)
        {
            return _worldInstance[world].EntIdx[entity];
        }

        private static void InitWorldImpl(int world)
        {
            _worldInstance[world] = new WorldInstance();
        }

        private static void RemoveWorldImpl(int world)
        {
            _worldInstance.Remove(world);
        }

        private static int CreateImpl(int world, int entity, int parent, Vector3f position, Vector3f rotation,
            Vector3f scale)
        {
            var world_instance = _worldInstance[world];

            var idx = world_instance.Position.Count;
            world_instance.Position.Add(position);
            world_instance.Rotation.Add(rotation);
            world_instance.Scale.Add(scale);

            world_instance.Parent.Add(int.MaxValue);
            world_instance.FirstChild.Add(int.MaxValue);
            world_instance.NextSibling.Add(int.MaxValue);

            world_instance.World.Add(Matrix4f.Identity);

            world_instance.EntIdx[entity] = idx;

            if (parent != int.MaxValue)
            {
                var parentIdx = world_instance.EntIdx[parent];

                world_instance.Parent[idx] = parentIdx;

                if (!is_valid(world_instance.FirstChild[parentIdx]))
                {
                    world_instance.FirstChild[parentIdx] = idx;
                }
                else
                {
                    var firstChildIdx = world_instance.FirstChild[parentIdx];

                    world_instance.FirstChild[parentIdx] = idx;
                    world_instance.NextSibling[idx] = firstChildIdx;
                }

                world_instance.Parent[idx] = parentIdx;
            }

            return idx;
        }

        private static bool is_valid(int idx)
        {
            return idx != int.MaxValue;
        }

        private static void Transform(int world, int idx, Matrix4f parent)
        {
            var world_instance = _worldInstance[world];

            var pos = world_instance.Position[idx];
            var rot = world_instance.Rotation[idx];
            var sca = world_instance.Scale[idx]; // TODO: !!!

            var local = Matrix4f.CreateFromYawPitchRoll(rot.X, rot.Y, rot.Z);
            local.M41 = pos.X;
            local.M42 = pos.Y;
            local.M43 = pos.Z;

            world_instance.World[idx] = local*parent;

            var child = world_instance.FirstChild[idx];

            while (is_valid(child))
            {
                Transform(world, child, world_instance.World[idx]);
                child = world_instance.NextSibling[child];
            }
        }

        private static void Spawner(int world, int[] ent_ids, int[] ents_parent, MessagePackObjectDictionary[] data)
        {
            for (var i = 0; i < ent_ids.Length; ++i)
            {
                var pos = data[i]["position"].AsList();
                var rot = data[i]["rotation"].AsList();
                var sca = data[i]["scale"].AsList();

                var position = new Vector3f {X = pos[0].AsSingle(), Y = pos[1].AsSingle(), Z = pos[2].AsSingle()};
                var rotation = new Vector3f {X = rot[0].AsSingle(), Y = rot[1].AsSingle(), Z = rot[2].AsSingle()};
                var scale = new Vector3f {X = sca[0].AsSingle(), Y = sca[1].AsSingle(), Z = sca[2].AsSingle()};

                Create(world, ent_ids[i],
                    ents_parent[i] != int.MaxValue ? ent_ids[ents_parent[i]] : int.MaxValue, position, rotation, scale);
            }

            for (var i = 0; i < ent_ids.Length; ++i)
            {
                Transform(world, getIdx(world, ent_ids[i]), Matrix4f.Identity);
            }
        }

        private static void Compiler(YamlMapping body, ConsoleServer.ResponsePacker packer)
        {
            var position = body["position"] as YamlSequence;
            var rotation = body["rotation"] as YamlSequence;
            var scale = body["scale"] as YamlSequence;

            packer.PackMapHeader(3);

            packer.Pack("position");
            packer.PackArrayHeader(3);
            packer.Pack(float.Parse(((YamlScalar) position[0]).Value, CultureInfo.InvariantCulture));
            packer.Pack(float.Parse(((YamlScalar) position[1]).Value, CultureInfo.InvariantCulture));
            packer.Pack(float.Parse(((YamlScalar) position[2]).Value, CultureInfo.InvariantCulture));

            packer.Pack("rotation");
            packer.PackArrayHeader(3);
            packer.Pack(float.Parse(((YamlScalar) rotation[0]).Value, CultureInfo.InvariantCulture));
            packer.Pack(float.Parse(((YamlScalar) rotation[1]).Value, CultureInfo.InvariantCulture));
            packer.Pack(float.Parse(((YamlScalar) rotation[2]).Value, CultureInfo.InvariantCulture));

            packer.Pack("scale");
            packer.PackArrayHeader(3);
            packer.Pack(float.Parse(((YamlScalar) scale[0]).Value, CultureInfo.InvariantCulture));
            packer.Pack(float.Parse(((YamlScalar) scale[1]).Value, CultureInfo.InvariantCulture));
            packer.Pack(float.Parse(((YamlScalar) scale[2]).Value, CultureInfo.InvariantCulture));
        }

        private static void InitImpl()
        {
#if CETECH_DEVELOP
            ComponentSystem.RegisterCompiler(StringId.FromString("transform"), Compiler, 1);
#endif
            ComponentSystem.RegisterSpawner(StringId.FromString("transform"), Spawner);
        }

        private static void ShutdownImpl()
        {
        }

        private static Vector3f GetPositionImpl(int world, int transform)
        {
            var world_instance = _worldInstance[world];
            return world_instance.Position[transform];
        }

        private static Vector3f GetRotationImpl(int world, int transform)
        {
            var world_instance = _worldInstance[world];
            return world_instance.Rotation[transform];
        }

        private static Vector3f GetScaleImpl(int world, int transform)
        {
            var world_instance = _worldInstance[world];
            return world_instance.Scale[transform];
        }

        private static Matrix4f GetWorldMatrixImpl(int world, int transform)
        {
            var world_instance = _worldInstance[world];
            return world_instance.World[transform];
        }

        private static void SetPositionImpl(int world, int transform, Vector3f pos)
        {
            var world_instance = _worldInstance[world];
            var parent_idx = world_instance.Parent[transform];
            var parent = parent_idx != int.MaxValue ? world_instance.World[parent_idx] : Matrix4f.Identity;

            world_instance.Position[transform] = pos;
            Transform(world, transform, parent);
        }

        private static void SetRotationImpl(int world, int transform, Vector3f rot)
        {
            var world_instance = _worldInstance[world];
            var parent_idx = world_instance.Parent[transform];
            var parent = parent_idx != int.MaxValue ? world_instance.World[parent_idx] : Matrix4f.Identity;

            world_instance.Rotation[transform] = rot;
            Transform(world, transform, parent);
        }

        private static void SetScaleImpl(int world, int transform, Vector3f scale)
        {
            var world_instance = _worldInstance[world];
            var parent_idx = world_instance.Parent[transform];
            var parent = parent_idx != int.MaxValue ? world_instance.World[parent_idx] : Matrix4f.Identity;

            world_instance.Scale[transform] = scale;
            Transform(world, transform, parent);
        }

        private static void LinkImpl(int world, int parent, int child)
        {
            var world_instance = _worldInstance[world];

            var parent_idx = getIdx(world, parent);
            var child_idx = getIdx(world, child);

            world_instance.Parent[child_idx] = parent_idx;

            var tmp = world_instance.FirstChild[parent_idx];

            world_instance.FirstChild[parent_idx] = child_idx;
            world_instance.NextSibling[child_idx] = tmp;

            var p = parent_idx != int.MaxValue ? world_instance.World[parent_idx] : Matrix4f.Identity;
            Transform(world, parent_idx, p); // TODO:
            Transform(world, child_idx, GetWorldMatrix(world, GetTranform(world, parent)));
        }

        private static int GetTranformImpl(int world, int entity)
        {
            return getIdx(world, entity);
        }

        private class WorldInstance
        {
            public readonly Dictionary<int, int> EntIdx;
            public readonly List<int> FirstChild;
            public readonly List<int> NextSibling;

            public readonly List<int> Parent;


            public readonly List<Vector3f> Position;
            public readonly List<Vector3f> Rotation;
            public readonly List<Vector3f> Scale;

            public readonly List<Matrix4f> World;

            public WorldInstance()
            {
                NextSibling = new List<int>();
                EntIdx = new Dictionary<int, int>();
                Position = new List<Vector3f>();
                Rotation = new List<Vector3f>();
                Scale = new List<Vector3f>();
                Parent = new List<int>();
                FirstChild = new List<int>();
                World = new List<Matrix4f>();
            }
        }
    }
}