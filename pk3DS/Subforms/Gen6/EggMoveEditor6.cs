﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;
using pk3DS.Properties;

namespace pk3DS
{
    public partial class EggMoveEditor6 : Form
    {
        public EggMoveEditor6(byte[][] infiles)
        {
            InitializeComponent();
            files = infiles;
            string[] specieslist = Main.getText(TextName.SpeciesNames);
            specieslist[0] = movelist[0] = "";

            string[] sortedspecies = (string[])specieslist.Clone();
            Array.Resize(ref sortedspecies, Main.Config.MaxSpeciesID); Array.Sort(sortedspecies);
            setupDGV();

            var newlist = new List<Util.cbItem>();
            for (int i = 1; i < Main.Config.MaxSpeciesID; i++) // add all species
                newlist.Add(new Util.cbItem { Text = sortedspecies[i], Value = Array.IndexOf(specieslist, sortedspecies[i]) });

            CB_Species.DisplayMember = "Text";
            CB_Species.ValueMember = "Value";
            CB_Species.DataSource = newlist;
            CB_Species.SelectedIndex = 0;
        }
        private readonly byte[][] files;
        private int entry = -1;
        private readonly string[] movelist = Main.getText(TextName.MoveNames);
        private bool dumping;
        private void setupDGV()
        {
            string[] sortedmoves = (string[])movelist.Clone();
            Array.Sort(sortedmoves);
            DataGridViewComboBoxColumn dgvMove = new DataGridViewComboBoxColumn();
            {
                dgvMove.HeaderText = "Move";
                dgvMove.DisplayIndex = 0;
                for (int i = 0; i < movelist.Length; i++)
                    dgvMove.Items.Add(sortedmoves[i]); // add only the Names

                dgvMove.Width = 135;
                dgvMove.FlatStyle = FlatStyle.Flat;
            }
            dgv.Columns.Add(dgvMove);
        }

        private EggMoves pkm = new EggMoves6(new byte[0]);
        private void getList()
        {
            entry = Util.getIndex(CB_Species);

            int[] specForm = Main.Config.Personal.getSpeciesForm(entry);
            string filename = "_" + specForm[0] + (entry > 721 ? "_" + (specForm[1] + 1) : "");
            PB_MonSprite.Image = (Bitmap)Resources.ResourceManager.GetObject(filename);

            dgv.Rows.Clear();
            byte[] input = files[entry];
            if (input.Length == 0) return;
            pkm = new EggMoves6(input);
            if (pkm.Count < 1) { files[entry] = new byte[0]; return; }
            dgv.Rows.Add(pkm.Count);

            // Fill Entries
            for (int i = 0; i < pkm.Count; i++)
                dgv.Rows[i].Cells[0].Value = movelist[pkm.Moves[i]];

            dgv.CancelEdit();
        }
        private void setList()
        {
            if (entry < 1 || dumping) return;
            List<int> moves = new List<int>();
            for (int i = 0; i < dgv.Rows.Count - 1; i++)
            {
                int move = Array.IndexOf(movelist, dgv.Rows[i].Cells[0].Value);
                if (move > 0 && !moves.Contains((ushort)move)) moves.Add(move);
            }
            pkm.Moves = moves.ToArray();

            files[entry] = pkm.Write();
        }

        private void changeEntry(object sender, EventArgs e)
        {
            setList();
            getList();
        }

        private void B_RandAll_Click(object sender, EventArgs e)
        {
            /*
             * 3111 Egg Moves Learned by 290 Species (10.73 avg)
             * 18 is the most
             * 1000 moves learned were STAB (32.1%)
             */
            Random rnd = new Random();

            ushort[] HMs = { 15, 19, 57, 70, 127, 249, 291 };
            ushort[] TMs = { };
            if (CHK_HMs.Checked && Main.ExeFSPath != null)
                TMHMEditor6.getTMHMList(Main.Config.ORAS, ref TMs, ref HMs);

            int[] banned = new int[HMs.Length];
            for (int i = 0; i < banned.Length; i++)
                banned[i] = HMs[i];
            banned = banned.Concat(new[] { 165, 621 }).ToArray(); // Struggle, Hyperspace Fury

            // Move Stats
            Move[] moveTypes = Main.Config.Moves;

            // Personal Stats

            // Set up Randomized Moves
            int[] randomMoves = Enumerable.Range(1, movelist.Length - 1).Select(i => i).ToArray();
            Util.Shuffle(randomMoves);
            int ctr = 0;
            for (int i = 0; i < CB_Species.Items.Count; i++)
            {
                CB_Species.SelectedIndex = i; // Get new Species
                int count = dgv.Rows.Count - 1;
                int species = Util.getIndex(CB_Species);
                if (count == 0)
                    continue;

                if (CHK_Expand.Checked && (int)NUD_Moves.Value > count)
                    dgv.Rows.AddCopies(count, (int)NUD_Moves.Value - count);
                for (int j = 0; j < dgv.Rows.Count - 1; j++)
                {
                    // Assign New Moves
                    bool forceSTAB = CHK_STAB.Checked && rnd.Next(0, 99) < NUD_STAB.Value;
                    int move = Randomizer.getRandomSpecies(ref randomMoves, ref ctr);
                    while ( // Move is invalid
                        (!CHK_HMs.Checked && banned.Contains(move)) // HM Moves Not Allowed
                        || (forceSTAB && // STAB is required
                            !Main.Config.Personal[species].Types.Contains(moveTypes[move].Type))
                        )
                    {
                        move = Randomizer.getRandomSpecies(ref randomMoves, ref ctr);
                    }

                    // Assign Move
                    dgv.Rows[j].Cells[0].Value = movelist[move];
                }
            }
            CB_Species.SelectedIndex = 0;
            Util.Alert("All Pokemon's Egg Moves have been randomized!");
        }
        private void B_Dump_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes != Util.Prompt(MessageBoxButtons.YesNo, "Dump all Egg Moves to Text File?"))
                return;

            dumping = true;
            string result = "";
            for (int i = 0; i < CB_Species.Items.Count; i++)
            {
                CB_Species.SelectedIndex = i; // Get new Species
                result += "======" + Environment.NewLine + entry + " " + CB_Species.Text + Environment.NewLine + "======" + Environment.NewLine;
                for (int j = 0; j < dgv.Rows.Count - 1; j++)
                    result += dgv.Rows[j].Cells[0].Value + Environment.NewLine;

                result += Environment.NewLine;
            }
            SaveFileDialog sfd = new SaveFileDialog {FileName = "Egg Moves.txt", Filter = "Text File|*.txt"};

            SystemSounds.Asterisk.Play();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string path = sfd.FileName;
                File.WriteAllText(path, result, Encoding.Unicode);
            }
            dumping = false;
        }

        private void formClosing(object sender, FormClosingEventArgs e)
        {
            setList();
        }
        private void calcStats()
        {
            Move[] MoveData = Main.Config.Moves;
            
            int movectr = 0;
            int max = 0;
            int spec = 0;
            int stab = 0;
            int species = 0;
            for (int i = 0; i < Main.Config.MaxSpeciesID; i++)
            {
                byte[] movedata = files[i];
                if (movedata.Length <= 2) continue;
                int movecount = BitConverter.ToUInt16(movedata, 0);
                if (movecount == 65535 || movecount < 0)
                    continue;
                species++;
                movectr += movecount; // Average Moves
                if (max < movecount) { max = movecount; spec = i; } // Max Moves (and species)
                for (int m = 1; m < movedata.Length / 2; m++)
                {
                    int move = BitConverter.ToUInt16(movedata, m * 2);
                    if (move == 65535)
                    {
                        movectr--;
                        continue;
                    }
                    if (Main.Config.Personal[species].Types.Contains(MoveData[move].Type))
                        stab++;
                }
            }
            Util.Alert(
                $"Moves Learned: {movectr}\r\nMost Learned: {max} @ {spec}\r\nSTAB Count: {stab}\r\nSpecies with EggMoves: {species}");
        }
    }
}