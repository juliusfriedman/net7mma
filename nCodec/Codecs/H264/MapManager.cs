﻿using nVideo.Codecs.H264;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public class MapManager
    {
        private SeqParameterSet sps;
        private PictureParameterSet pps;
        private MBToSliceGroupMap mbToSliceGroupMap;
        private int prevSliceGroupChangeCycle;

        public MapManager(SeqParameterSet sps, PictureParameterSet pps)
        {
            this.sps = sps;
            this.pps = pps;
            this.mbToSliceGroupMap = buildMap(sps, pps);
        }

        private MBToSliceGroupMap buildMap(SeqParameterSet sps, PictureParameterSet pps)
        {
            int numGroups = pps.num_slice_groups_minus1 + 1;

            if (numGroups > 1)
            {
                int[] map;
                int picWidthInMbs = sps.pic_width_in_mbs_minus1 + 1;
                int picHeightInMbs = Utility.getPicHeightInMbs(sps);

                if (pps.slice_group_map_type == 0)
                {
                    int[] runLength = new int[numGroups];
                    for (int i = 0; i < numGroups; i++)
                    {
                        runLength[i] = pps.run_length_minus1[i] + 1;
                    }
                    map = SliceGroupMapBuilder.buildInterleavedMap(picWidthInMbs, picHeightInMbs, runLength);
                }
                else if (pps.slice_group_map_type == 1)
                {
                    map = SliceGroupMapBuilder.buildDispersedMap(picWidthInMbs, picHeightInMbs, numGroups);
                }
                else if (pps.slice_group_map_type == 2)
                {
                    map = SliceGroupMapBuilder.buildForegroundMap(picWidthInMbs, picHeightInMbs, numGroups, pps.top_left,
                            pps.bottom_right);
                }
                else if (pps.slice_group_map_type >= 3 && pps.slice_group_map_type <= 5)
                {
                    return null;
                }
                else if (pps.slice_group_map_type == 6)
                {
                    map = pps.slice_group_id;
                }
                else
                {
                    throw new Exception("Unsupported slice group map type");
                }

                return buildMapIndices(map, numGroups);
            }

            return null;
        }

        private MBToSliceGroupMap buildMapIndices(int[] map, int numGroups)
        {
            int[] ind = new int[numGroups];
            int[] indices = new int[map.Length];

            for (int i = 0; i < map.Length; i++)
            {
                indices[i] = ind[map[i]]++;
            }

            int[][] inverse = new int[numGroups][];
            for (int i = 0; i < numGroups; i++)
            {
                inverse[i] = new int[ind[i]];
            }
            ind = new int[numGroups];
            for (int i = 0; i < map.Length; i++)
            {
                int sliceGroup = map[i];
                inverse[sliceGroup][ind[sliceGroup]++] = i;
            }

            return new MBToSliceGroupMap(map, indices, inverse);
        }

        private void updateMap(SliceHeader sh)
        {
            int mapType = pps.slice_group_map_type;
            int numGroups = pps.num_slice_groups_minus1 + 1;

            if (numGroups > 1 && mapType >= 3 && mapType <= 5
                    && (sh.slice_group_change_cycle != prevSliceGroupChangeCycle || mbToSliceGroupMap == null))
            {

                prevSliceGroupChangeCycle = sh.slice_group_change_cycle;

                int picWidthInMbs = sps.pic_width_in_mbs_minus1 + 1;
                int picHeightInMbs = Utility.getPicHeightInMbs(sps);
                int picSizeInMapUnits = picWidthInMbs * picHeightInMbs;
                int mapUnitsInSliceGroup0 = sh.slice_group_change_cycle * (pps.slice_group_change_rate_minus1 + 1);
                mapUnitsInSliceGroup0 = mapUnitsInSliceGroup0 > picSizeInMapUnits ? picSizeInMapUnits
                        : mapUnitsInSliceGroup0;

                int sizeOfUpperLeftGroup = (pps.slice_group_change_direction_flag ? (picSizeInMapUnits - mapUnitsInSliceGroup0)
                        : mapUnitsInSliceGroup0);

                int[] map;
                if (mapType == 3)
                {
                    map = SliceGroupMapBuilder.buildBoxOutMap(picWidthInMbs, picHeightInMbs,
                            pps.slice_group_change_direction_flag, mapUnitsInSliceGroup0);
                }
                else if (mapType == 4)
                {
                    map = SliceGroupMapBuilder.buildRasterScanMap(picWidthInMbs, picHeightInMbs, sizeOfUpperLeftGroup,
                            pps.slice_group_change_direction_flag);
                }
                else
                {
                    map = SliceGroupMapBuilder.buildWipeMap(picWidthInMbs, picHeightInMbs, sizeOfUpperLeftGroup,
                            pps.slice_group_change_direction_flag);
                }

                this.mbToSliceGroupMap = buildMapIndices(map, numGroups);
            }
        }

        public Mapper getMapper(SliceHeader sh)
        {
            updateMap(sh);
            int firstMBInSlice = sh.first_mb_in_slice;
            if (pps.num_slice_groups_minus1 > 0)
            {

                return new PrebuiltMBlockMapper(mbToSliceGroupMap, firstMBInSlice, sps.pic_width_in_mbs_minus1 + 1);
            }
            else
            {
                return new FlatMBlockMapper(sps.pic_width_in_mbs_minus1 + 1, firstMBInSlice);
            }
        }
    }
}
