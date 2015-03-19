using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MapAround.Geometry;
using MapAround.IO.Handlers;
using MapAround.Mapping;
using MapAround.DataProviders;

namespace MapAround.IO.Handlers
{
    class MapPointShapeHandler : ShapeHandler
    {
        /// <summary>
        /// Читает запись представляющую точку.
        /// </summary>
        /// <param name="blk">Входной поток</param>
        /// <param name="record">Запись Shape-файла в которую будет помещена прочитанная информация</param>
        /// <param name="bounds">Ограничивающий прямоугольник, с которым должен пересекаться ограничивающий прямоугольник записи</param>
        public override bool Read(/*BigEndianBinaryReader*/TABRawBlock blk, BoundingRectangle bounds, ShapeFileRecord record, double scale)
        {

            ICoordinate p = PlanimetryEnvironment.NewCoordinate();
            p.X = blk.ReadInt32() * scale;
            p.Y = blk.ReadInt32() * scale;

            if (bounds != null && !bounds.IsEmpty() && !bounds.ContainsPoint(p))
                return false;

            record.Points.Add(p);

            record.MinX = p.X;
            record.MinY = p.Y;
            record.MaxX = record.MinX;
            record.MaxY = record.MinY;

            return true;
        }

    }
}
