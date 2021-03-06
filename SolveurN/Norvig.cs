﻿using System;
using System.Collections.Generic;
using System.Text;
using Noyau;
using System.Linq;
using System.IO;

namespace SolverN
{
    public class Norvig : ISudokuSolveur
    {

        static string rows = "ABCDEFGHI";
        static string cols = "123456789";
        static string digits = "123456789";
        static string[] squares = cross(rows, cols);
        static Dictionary<string, IEnumerable<string>> peers;
        static Dictionary<string, IGrouping<string, string[]>> units;

        static Norvig()
        {
            /*
        * unitlist = ([cross(rows, c) for c in cols] +
        *           [cross(r, cols) for r in rows] +
        *           [cross(rs, cs) for rs in ('ABC','DEF','GHI') for cs in ('123','456','789')])
        */
            var unitlist = ((from c in cols select cross(rows, c.ToString()))
                               .Concat(from r in rows select cross(r.ToString(), cols))
                               .Concat(from rs in (new[] { "ABC", "DEF", "GHI" }) from cs in (new[] { "123", "456", "789" }) select cross(rs, cs)));

            /*
             * units = dict((s, [u for u in unitlist if s in u]) 
             *   for s in squares)
             */
            units = (from s in squares from u in unitlist where u.Contains(s) group u by s into g select g).ToDictionary(g => g.Key);

            /*
             * peers = dict((s, set(s2 for u in units[s] for s2 in u if s2 != s))
             *   for s in squares)
             */
            peers = (from s in squares from u in units[s] from s2 in u where s2 != s group s2 by s into g select g).ToDictionary(g => g.Key, g => g.Distinct());

        }

        Sudoku Solve(Sudoku s)
        {
            //print_board(s);
            int j = 0;
            int k = 0;
            Console.WriteLine(s);
            var sol = search(parse_grid(string.Join("", s.Cells.Select(x => x.ToString()))));


            foreach (KeyValuePair<string, string> kvp in sol)
            {
                if (k == 9)
                {
                    j++;
                    k = 0;
                }
                s.SetCell(j, k, Int32.Parse(kvp.Value));
                k++;
            }

            return s;

        }

        static string[] cross(string A, string B)
        {
            return (from a in A from b in B select "" + a + b).ToArray();
        }

        Dictionary<string, string> parse_grid(string grid)
        {
            var grid2 = from c in grid where "0.-123456789".Contains(c) select c;
            var values = squares.ToDictionary(s => s, s => digits); //To start, every square can be any digit

            foreach (var sd in zip(squares, (from s in grid select s.ToString()).ToArray()))
            {
                var s = sd[0];
                var d = sd[1];
                //Console.WriteLine(s,d);

                if (digits.Contains(d) && assign(values, s, d) == null)
                {
                    return null;
                }
            }

            return values;
        }


        Dictionary<string, string> search(Dictionary<string, string> values)
        {
            if (values == null)
            {
                return null; // Failed earlier
            }
            if (all(from s in squares select values[s].Length == 1 ? "" : null))
            {
                return values; // Solved!
            }

            // Chose the unfilled square s with the fewest possibilities
            var s2 = (from s in squares where values[s].Length > 1 orderby values[s].Length ascending select s).First();

            return some(from d in values[s2]
                        select search(assign(new Dictionary<string, string>(values), s2, d.ToString())));
        }


        Dictionary<string, string> assign(Dictionary<string, string> values, string s, string d)
        {
            if (all(
                    from d2 in values[s]
                    where d2.ToString() != d
                    select eliminate(values, s, d2.ToString())))
            {
                return values;
            }
            return null;
        }

        static bool all<T>(IEnumerable<T> seq)
        {
            foreach (var e in seq)
            {
                if (e == null) return false;
            }
            return true;
        }

        static T some<T>(IEnumerable<T> seq)
        {
            foreach (var e in seq)
            {
                if (e != null) return e;
            }
            return default;
        }


        Dictionary<string, string> eliminate(Dictionary<string, string> values, string s, string d)
        {
            if (!values[s].Contains(d))
            {
                return values;
            }
            values[s] = values[s].Replace(d, "");
            if (values[s].Length == 0)
            {
                return null; //Contradiction: removed last value
            }
            else if (values[s].Length == 1)
            {
                //If there is only one value (d2) left in square, remove it from peers
                var d2 = values[s];
                if (!all(from s2 in peers[s] select eliminate(values, s2, d2)))
                {
                    return null;
                }
            }

            //Now check the places where d appears in the units of s
            foreach (var u in units[s])
            {
                var dplaces = from s2 in u where values[s2].Contains(d) select s2;
                if (dplaces.Count() == 0)
                {
                    return null;
                }
                else if (dplaces.Count() == 1)
                {
                    // d can only be in one place in unit; assign it there
                    if (assign(values, dplaces.First(), d) == null)
                    {
                        return null;
                    }
                }
            }
            return values;
        }

        static string[][] zip(string[] A, string[] B)
        {
            var n = Math.Min(A.Length, B.Length);
            string[][] sd = new string[n][];
            for (var i = 0; i < n; i++)
            {
                sd[i] = new string[] { A[i].ToString(), B[i].ToString() };
            }
            return sd;
        }


        // fonction qui resoud le sudoku 
        public Sudoku ResoudreSudoku(Sudoku s)
        {

            return Solve(s);


        }


    }
}