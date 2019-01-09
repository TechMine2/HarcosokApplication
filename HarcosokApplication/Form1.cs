using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace HarcosokApplication
{
    public partial class Form_Alap : Form
    {

        public Form_Alap()
        {
            InitializeComponent();
            if (!EllenorizDBKapcsolat())
            {
                MessageBox.Show("Nincs adatbázis. A program bezáródik.", "MYSQL Hiba");
                if (Application.MessageLoop)
                {
                    Application.Exit();
                }
                else
                {
                    Environment.Exit(1);
                }
            }
            HarcosFrissit();
        }

        public bool EllenorizDBKapcsolat()
        {
            bool vanKapcsolat = false;
            using (var conn = new MySqlConnection("SERVER=localhost;Database=cs_harcosok;UID=root;PASSWORD=;"))
            {
                try
                {
                    conn.Open();
                    vanKapcsolat = true;
                    conn.Close();
                }
                catch (Exception)
                {
                    vanKapcsolat = false;
                }

            }

            return vanKapcsolat;
        }

        private void button_HarcosLetrehoz_Click(object sender, EventArgs e)
        {
            if (textBoxHarcosNeve.Text.ToString() != "")
            {
                string harcosNev = textBoxHarcosNeve.Text.ToString();
                using (var conn = new MySqlConnection("SERVER=localhost;Database=cs_harcosok;UID=root;PASSWORD=;"))
                {
                    conn.Open();
                    var tablaLetrehoz = conn.CreateCommand();
                    tablaLetrehoz.CommandText = "CREATE TABLE IF NOT exists harcosok (id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY, nev VARCHAR(32) NOT NULL, letrehozas TIMESTAMP)";
                    tablaLetrehoz.ExecuteNonQuery();

                    var ellenorzes = conn.CreateCommand();
                    ellenorzes.CommandText = "SELECT COUNT(*) FROM harcosok WHERE nev=@nev";
                    ellenorzes.Parameters.AddWithValue("@nev", harcosNev);
                    var darab = (long)ellenorzes.ExecuteScalar();
                    if (darab != 0)
                    {
                        MessageBox.Show("Ilyen nevű harcos már létezik.", "Hiba");
                        return;
                    }
                    else
                    {
                        var harcosLetrehoz = conn.CreateCommand();
                        harcosLetrehoz.CommandText = "INSERT INTO harcosok (nev, letrehozas) VALUES (@nev, now())";
                        harcosLetrehoz.Parameters.AddWithValue("@nev", harcosNev);
                        int sikeresE = harcosLetrehoz.ExecuteNonQuery();
                        if (sikeresE > 0)
                        {
                            HarcosFrissit();
                        }
                        else
                        {
                            MessageBox.Show("Sikertelen harcos felvétel", "Hiba");
                        }
                    }

                    conn.Close();
                }
            }
            else
            {
                MessageBox.Show("Nincs megadva a harcos neve!", "Hiba");
            }
        }

        public void HarcosFrissit()
        {
            using (var conn = new MySqlConnection("SERVER=localhost;Database=cs_harcosok;UID=root;PASSWORD=;"))
            {
                conn.Open();
                
                var ellenoriz = conn.CreateCommand();
                ellenoriz.CommandText = "SHOW TABLES LIKE 'harcosok'";
                MySqlDataReader eredmeny = ellenoriz.ExecuteReader();
                int szam = 0;
                while (eredmeny.Read())
                {
                    szam++;
                }
                eredmeny.Close();
                if (szam==0)
                {
                    return;
                }

                var harcosok = conn.CreateCommand();
                harcosok.CommandText = "SELECT nev, letrehozas FROM harcosok";
                MySqlDataReader eredmenyek = harcosok.ExecuteReader();
                comboBox_hasznalo.Items.Clear();
                listBoxHarcosok.Items.Clear();
                while (eredmenyek.Read())
                {
                    comboBox_hasznalo.Items.Add(eredmenyek["nev"]);
                    listBoxHarcosok.Items.Add("" + eredmenyek["nev"] + " - " + eredmenyek["letrehozas"]);
                }

                conn.Close();
            }
        }

        private void button_KepessegHozzaad_Click(object sender, EventArgs e)
        {
            if (textBoxKepessegNeve.Text.ToString() != "" && textBoxKepessegHozzaadLeiras.Text.ToString() != "" && comboBox_hasznalo.SelectedIndex > -1 && textBoxKepessegHozzaadLeiras.Text.ToString().Length<=128)
            {
                string kepessegNev = textBoxKepessegNeve.Text.ToString();
                string kepessegLeiras = textBoxKepessegHozzaadLeiras.Text.ToString();
                string kepessegHasznalo = comboBox_hasznalo.SelectedItem.ToString();

                using (var conn = new MySqlConnection("SERVER=localhost;Database=cs_harcosok;UID=root;PASSWORD=;"))
                {
                    conn.Open();
                    var tablaLetrehoz = conn.CreateCommand();
                    tablaLetrehoz.CommandText = "CREATE TABLE IF NOT exists kepessegek (id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY, nev VARCHAR(32) NOT NULL, leiras VARCHAR(128) NOT NULL, harcos_id INT NOT NULL)";
                    tablaLetrehoz.ExecuteNonQuery();

                    var harcosIDLeker = conn.CreateCommand();
                    harcosIDLeker.CommandText = "SELECT id FROM harcosok WHERE nev=@nev";
                    harcosIDLeker.Parameters.AddWithValue("@nev", kepessegHasznalo);
                    MySqlDataReader eredmeny = harcosIDLeker.ExecuteReader();
                    eredmeny.Read();
                    string harcosID = eredmeny["id"].ToString();
                    eredmeny.Close();

                    var kepessegLetrehoz = conn.CreateCommand();
                    kepessegLetrehoz.CommandText = "INSERT INTO kepessegek (nev, leiras, harcos_id) VALUES (@nev, @leiras, @harcos_id)";
                    kepessegLetrehoz.Parameters.AddWithValue("@nev", kepessegNev);
                    kepessegLetrehoz.Parameters.AddWithValue("@leiras", kepessegLeiras);
                    kepessegLetrehoz.Parameters.AddWithValue("@harcos_id", harcosID);
                    kepessegLetrehoz.ExecuteNonQuery();

                    conn.Close();
                }
            }
            else
            {
                MessageBox.Show("Nincs megadva minden a képesség hozzáadásához vagy a leírás hosszabb mint 128 karakter!", "Hiba");
            }
        }

        private void listBoxHarcosok_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxHarcosok.SelectedItem != null)
            {
                KepessegFrissit();
            }
        }

        List<int> kepessegIDk = new List<int>();

        public void KepessegFrissit()
        {
            string[] szetszedett = listBoxHarcosok.SelectedItem.ToString().Split('-');
            //utolsó karaktert (itt szóköz) törli le
            string harcosNeve = szetszedett[0].Remove(szetszedett[0].Length - 1, 1);
            //MessageBox.Show(harcosNeve, "Kiválasztott harcos");
            using (var conn = new MySqlConnection("SERVER=localhost;Database=cs_harcosok;UID=root;PASSWORD=;"))
            {
                conn.Open();

                var ellenoriz = conn.CreateCommand();
                ellenoriz.CommandText = "SHOW TABLES LIKE 'kepessegek'";
                MySqlDataReader eredmeny = ellenoriz.ExecuteReader();
                int szam = 0;
                while (eredmeny.Read())
                {
                    szam++;
                }
                eredmeny.Close();
                if (szam == 0)
                {
                    return;
                }

                var harcos_id = conn.CreateCommand();
                harcos_id.CommandText = "SELECT id FROM harcosok WHERE nev=@nev";
                harcos_id.Parameters.AddWithValue("@nev", harcosNeve);
                eredmeny = harcos_id.ExecuteReader();
                eredmeny.Read();
                int harcosID = Convert.ToInt32(eredmeny[0]);
                eredmeny.Close();

                var kepessegListazas = conn.CreateCommand();
                kepessegListazas.CommandText = "SELECT id, nev FROM kepessegek WHERE harcos_id=@harcos_id";
                kepessegListazas.Parameters.AddWithValue("@harcos_id", harcosID);
                eredmeny = kepessegListazas.ExecuteReader();
                listBoxKepessegek.Items.Clear();
                kepessegIDk.Clear();
                while (eredmeny.Read())
                {
                    listBoxKepessegek.Items.Add(eredmeny["nev"]);
                    kepessegIDk.Add(Convert.ToInt32(eredmeny["id"]));
                }
                conn.Close();

                textBoxKepessegLeirasa.Text = "";
            }
        }

        private void listBoxKepessegek_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxKepessegek.SelectedItem != null)
            {

                string kepessegNeve = listBoxKepessegek.SelectedItem.ToString();
                //MessageBox.Show(kepessegNeve, "Kiválasztott képesség");
                using (var conn = new MySqlConnection("SERVER=localhost;Database=cs_harcosok;UID=root;PASSWORD=;"))
                {
                    conn.Open();

                    var ellenoriz = conn.CreateCommand();
                    ellenoriz.CommandText = "SHOW TABLES LIKE 'kepessegek'";
                    MySqlDataReader eredmeny = ellenoriz.ExecuteReader();
                    int szam = 0;
                    while (eredmeny.Read())
                    {
                        szam++;
                    }
                    eredmeny.Close();
                    if (szam == 0)
                    {
                        MessageBox.Show("Nincs képesség tábla", "MYSQL HIBA");
                        return;
                    }

                    var kepessegListazas = conn.CreateCommand();
                    kepessegListazas.CommandText = "SELECT leiras FROM kepessegek WHERE id=@kepessegID";
                    kepessegListazas.Parameters.AddWithValue("@kepessegID", kepessegIDk[listBoxKepessegek.SelectedIndex]);
                    eredmeny = kepessegListazas.ExecuteReader();
                    eredmeny.Read();
                    textBoxKepessegLeirasa.Text = eredmeny[0].ToString();
                    eredmeny.Close();

                    conn.Close();
                }

            }
        }

        private void buttonKepessegTorles_Click(object sender, EventArgs e)
        {
            if (listBoxKepessegek.SelectedItem != null)
            {
                using (var conn = new MySqlConnection("SERVER=localhost;Database=cs_harcosok;UID=root;PASSWORD=;"))
                {
                    conn.Open();

                    var torles = conn.CreateCommand();
                    torles.CommandText = "DELETE FROM `kepessegek` WHERE `kepessegek`.`nev` = @nev AND `kepessegek`.`leiras` = @leiras";
                    torles.Parameters.AddWithValue("@nev", listBoxKepessegek.SelectedItem.ToString());
                    torles.Parameters.AddWithValue("@leiras", textBoxKepessegLeirasa.Text.ToString());

                    torles.ExecuteNonQuery();
                    conn.Close();
                    KepessegFrissit();
                }
            }else
            {
                MessageBox.Show("Válassz ki egy képességet, amit törölni szeretnél.", "Hiba");
            }
        }

        private void buttonKepessegLeirasModosit_Click(object sender, EventArgs e)
        {
            if (listBoxKepessegek.SelectedItem != null)
            {
                using (var conn = new MySqlConnection("SERVER=localhost;Database=cs_harcosok;UID=root;PASSWORD=;"))
                {
                    conn.Open();

                    var modosit = conn.CreateCommand();
                    modosit.CommandText = "UPDATE kepessegek SET leiras=@ujleiras WHERE id = @id";
                    modosit.Parameters.AddWithValue("@id", kepessegIDk[listBoxKepessegek.SelectedIndex]);
                    modosit.Parameters.AddWithValue("@ujleiras", textBoxKepessegLeirasa.Text.ToString());
                    modosit.ExecuteNonQuery();
                    conn.Close();
                    KepessegFrissit();
                }
            }
            else
            {
                MessageBox.Show("Válassz ki egy képességet, amit módosítani szeretnél.", "Hiba");
            }
        }
    }
}
