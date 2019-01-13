﻿using ShenmueDKSharp.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models._MT5
{
    /// <summary>
    /// MT5 mesh data
    /// TODO: fix character model missing strips
    /// </summary>
    public class MT5Mesh
    {
        private MT5Node m_node;
        private MT5Node m_parentNode;

        uint Offset;

        uint PolyType;
        public uint VerticesOffset;
        public int VertexCount;
        public uint FacesOffset;
        public Vector3 MeshCenter;
        public float MeshDiameter;

        public List<Vertex> Vertices = new List<Vertex>();
        public List<MeshFace> Faces = new List<MeshFace>();

        /// <summary>
        /// All types based on sm1 asm
        /// </summary>
        public enum MT5MeshEntryType : ushort
        {
            //Skip 2 bytes
            Zero = 0x0000,

            //Skip 12 bytes
            Unknown_0E00 = 0x000E, //ignored
            Unknown_0F00 = 0x000F, //ignored

            //Skip 4 bytes
            Texture = 0x0009,
            Unknown_8000 = 0x0008, //ignored
            Unknown_A000 = 0x000A, //ignored
            Unknown_0B00 = 0x000B,
            StripAttrib_0200 = 0x0002,
            StripAtrrib_0300 = 0x0003,

            //faces (strips)
            Strip_1000 = 0x0010,
            Strip_1100 = 0x0011,
            Strip_1200 = 0x0012,
            Strip_1300 = 0x0013,
            Strip_1400 = 0x0014,

            Strip_1800 = 0x0018,
            Strip_1900 = 0x0019,
            Strip_1A00 = 0x001A,
            Strip_1B00 = 0x001B,
            Strip_1C00 = 0x001C,

            End = 0x8000
        }

        public MT5Mesh(BinaryReader reader, MT5Node node)
        {
            m_node = node;
            m_parentNode = (MT5Node)node.Parent;

            Offset = (uint)reader.BaseStream.Position;

            //Console.WriteLine("MeshOffset: {0}", Offset);

            PolyType = reader.ReadUInt32();
            VerticesOffset = reader.ReadUInt32();
            VertexCount = reader.ReadInt32();
            FacesOffset = reader.ReadUInt32();

            MeshCenter = new Vector3()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };
            MeshDiameter = reader.ReadSingle();

            //Read strips/faces
            reader.BaseStream.Seek(FacesOffset, SeekOrigin.Begin);

            //initialize states
            uint textureIndex = 0;
            Color4 stripColor = Color4.White;
            bool uvFlag = false;
            uint unknown_0B00 = 0; //polytype? uv mirror? (used when reading strip)

            //read strip functions
            while (reader.BaseStream.Position < reader.BaseStream.Length - 4)
            {
                ushort stripType = reader.ReadUInt16();
                if ((MT5MeshEntryType)stripType == MT5MeshEntryType.End) break;
                
                //Console.WriteLine("StripType: {0}", (MT5MeshEntryType)stripType);

                switch ((MT5MeshEntryType)stripType)
                {
                    case MT5MeshEntryType.Zero:
                        continue;
                    
                    //ignored by d3t
                    case MT5MeshEntryType.Unknown_0E00:
                    case MT5MeshEntryType.Unknown_0F00:
                        reader.BaseStream.Seek(10, SeekOrigin.Current);
                        continue;

                    //ignored by d3t
                    case MT5MeshEntryType.Unknown_8000:
                    case MT5MeshEntryType.Unknown_A000:
                        reader.BaseStream.Seek(2, SeekOrigin.Current);
                        continue;
                    
                    case MT5MeshEntryType.Unknown_0B00:
                        unknown_0B00 = reader.ReadUInt16();
                        //Console.WriteLine("0B00: {0:X}", unknown_0B00);
                        continue;

                    case MT5MeshEntryType.StripAttrib_0200:
                    case MT5MeshEntryType.StripAtrrib_0300:
                        uint size = reader.ReadUInt16();
                        long offset = reader.BaseStream.Position;

                        //Debug output whole attribute bytes
                        byte[] bytes = reader.ReadBytes((int)size);
                        StringBuilder hex = new StringBuilder(bytes.Length * 2);
                        foreach (byte a in bytes)
                        {
                            hex.AppendFormat("{0:X2}", a);
                        }
                        //Console.WriteLine(hex.ToString());
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

                        //first byte controls uv somehow
                        uint unknown = reader.ReadUInt32();
                        uvFlag = (unknown & 1) == 1; //TODO: use this flag

                        //skip unknown stuff
                        reader.BaseStream.Seek(3, SeekOrigin.Current);

                        //strip color
                        byte stripB = reader.ReadByte();
                        byte stripG = reader.ReadByte();
                        byte stripR = reader.ReadByte();
                        stripColor = new Color4(stripR, stripG, stripB, 255);

                        reader.BaseStream.Seek(offset + size, SeekOrigin.Begin);
                        continue;

                    case MT5MeshEntryType.Texture:
                        textureIndex = reader.ReadUInt16();
                        continue;

                    //Face strips
                    case MT5MeshEntryType.Strip_1000:
                    case MT5MeshEntryType.Strip_1100:
                    case MT5MeshEntryType.Strip_1200:
                    case MT5MeshEntryType.Strip_1300:
                    case MT5MeshEntryType.Strip_1400:
                    case MT5MeshEntryType.Strip_1800:
                    case MT5MeshEntryType.Strip_1900:
                    case MT5MeshEntryType.Strip_1A00:
                    case MT5MeshEntryType.Strip_1B00:
                    case MT5MeshEntryType.Strip_1C00:

                        bool hasUV = false;
                        bool hasColor = false;
                        if ((MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1200 ||
                            (MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1A00)
                        {
                            hasColor = true;
                        }
                        if ((MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1100 ||
                            (MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1900)
                        {
                            hasUV = true;
                        }
                        if ((MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1400 ||
                            (MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1C00)
                        {
                            hasUV = true;
                            hasColor = true;
                        }

                        reader.BaseStream.Seek(2, SeekOrigin.Current); //d3t skips this short value
                        ushort stripCount = reader.ReadUInt16();
                        if (stripCount == 0) continue;
            
                        for (int i = 0; i < stripCount; i++)
                        {
                            MeshFace face = new MeshFace();
                            face.Type = MeshFace.PrimitiveType.TriangleStrip;
                            face.TextureIndex = textureIndex;
                            face.StripColor = stripColor;

                            short stripLength = reader.ReadInt16();
                            if (stripLength < 0)
                            {
                                stripLength = Math.Abs(stripLength);
                            }

                            face.VertexIndices = new ushort[stripLength];
                            for (int j = 0; j < stripLength; j++)
                            {
                                short vertIndex = reader.ReadInt16();
                                while (vertIndex < 0)
                                {
                                    if (m_parentNode == null || m_parentNode.MeshData == null)
                                    {
                                        vertIndex = (short)(vertIndex + VertexCount);
                                    }
                                    else
                                    {
                                        //Offset parent vertex indices by own vertex count so we use the appended parents vertices
                                        vertIndex = (short)(VertexCount + vertIndex + m_parentNode.MeshData.VertexCount);
                                    }
                                }
                                face.VertexIndices[j] = (ushort)vertIndex;

                                float u = -1.0f;
                                float v = -1.0f;
                                if (hasUV)
                                {
                                    short texU = reader.ReadInt16();
                                    short texV = reader.ReadInt16();

                                    //UV/N normal-resolution 0 - 255
                                    //UVH high-resolution 0 - 1023
                                    u = texU / 1024.0f;
                                    v = texV / 1024.0f;
                                }

                                byte b = 255;
                                byte g = 255;
                                byte r = 255;
                                byte a = 255;
                                if (hasColor)
                                {
                                    //BGRA (8888) 32BPP
                                    b = reader.ReadByte();
                                    g = reader.ReadByte();
                                    r = reader.ReadByte();
                                    a = reader.ReadByte();
                                    //Console.WriteLine("R:{0} G:{1} B:{2} A:{3}", r, g, b, a);
                                }

                                //always add uv's and color like d3t did
                                face.UVs.Add(new Vector2(u, v));
                                face.Colors.Add(new Color4(r, g, b, a));
                            }
                            Faces.Add(face);
                        }
                        continue;

                    default:
                        //unknown type defaults to breaking
                        stripType = (ushort)MT5MeshEntryType.End;
                        break;
                }
                if ((MT5MeshEntryType)stripType == MT5MeshEntryType.End) break;
            }

            //Read vertices
            reader.BaseStream.Seek(VerticesOffset, SeekOrigin.Begin);
            for (int i = 0; i < VertexCount; i++)
            {
                Vector3 pos;
                pos.X = reader.ReadSingle();
                pos.Y = reader.ReadSingle();
                pos.Z = reader.ReadSingle();

                Vector3 norm;
                norm.X = reader.ReadSingle();
                norm.Y = reader.ReadSingle();
                norm.Z = reader.ReadSingle();

                Vertices.Add(new Vertex(pos, norm));
            }

            if (m_parentNode != null && m_parentNode.MeshData != null)
            {
                //Because for performance/memory saving the vertices from the parent can be used via negativ vertex indices
                //we just copy the parent vertices so we can use them with modified vertex offsets

                //Apply the inverted transform matrix of the node on vertices so they get canceled out by the final transform.
                Matrix4 matrix = m_node.GetTransformMatrixSelf().Inverted();
                foreach (Vertex vert in m_parentNode.MeshData.Vertices)
                {
                    Vertex v = new Vertex(vert);

                    v.Position = Vector3.TransformPosition(v.Position, matrix);
                    v.Normal = Vector3.TransformPosition(v.Normal, matrix);

                    Vertices.Add(v);
                }
            }
        }
    }
}
