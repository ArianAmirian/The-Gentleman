using TheGentleman.Core;

namespace TheGentleman.Data;

/// <summary>
/// Alla banor i spelet. Teckenförklaring:
///   #  vägg            @  spelarens start
///   D  låst dörr       k  nyckel
///   E  utgång          *  guldmynt
///   $  diamant         &gt; &lt; ^ v  vakt (pilen visar åt vilket håll den går)
/// Vill du lägga till en bana? Rita den här, spelet läser in den automatiskt.
/// </summary>
public static class LevelLibrary
{
    public static readonly IReadOnlyList<LevelData> All = new List<LevelData>
    {
        new("Ingången",
            "Ett tyst museum en söndagsnatt. Inga vakter än, perfekt för att öva.\n" +
            "Hitta nycklarna, lås upp dörrarna och ta diamanten. Gå sedan ut genom E.",
            Par: 60,
            Map: new[]
            {
                "##############################",
                "#@       #         k        E#",
                "#        #                   #",
                "#   *    D    *              #",
                "#        #      ######D###   #",
                "#   k    #      #  *  $  #   #",
                "#        #      #        #   #",
                "#        #      ##########   #",
                "##############################",
            }),

        new("Korridoren",
            "En vakt patrullerar korridoren. Han ser rakt fram, de röda prickarna.\n" +
            "Vänta på rätt ögonblick (mellanslag) och smit förbi när han tittar bort.",
            Par: 85,
            Map: new[]
            {
                "##############################",
                "#@         #       *        E#",
                "#   *      #                 #",
                "#          D                 #",
                "#    k     #                 #",
                "######## ###########  ########",
                "#<                          ##",
                "#### ######## ########### ####",
                "#      #         $  #    *   #",
                "#   *  #            #        #",
                "#      #            #        #",
                "##############################",
            }),

        new("Galleriet",
            "Två vakter går åt var sitt håll i galleriets hall.\n" +
            "Nyckeln till valvet ligger i rummet uppe till höger.",
            Par: 135,
            Map: new[]
            {
                "################################",
                "#@     #               #       #",
                "#      #   *       *   #   k   #",
                "#   *  #               #       #",
                "#### ########## ########### ####",
                "#>                             #",
                "#                             <#",
                "########D######### ######## ####",
                "#            #        #        #",
                "#     $      #    *   #     E  #",
                "#            #        #        #",
                "################################",
            }),

        new("Kontoret",
            "Bankens kontor. Två vakter i hallen och en i hisschaktet.\n" +
            "Diamanten ligger bakom en låst dörr längst ner i schaktet.",
            Par: 165,
            Map: new[]
            {
                "##################################",
                "#@      #       #v#       #  *   #",
                "#       #   *   # #       #      #",
                "#   k   D       # #   *   D      #",
                "#       #       # #       #   k  #",
                "####### ####### # ####### ####D###",
                "#>                              <#",
                "#### ############ ### ####### ####",
                "#       #       # #      #       #",
                "#   *   #   $   D #   *  #   E   #",
                "#       #       # #      #       #",
                "#       #       # #      #       #",
                "##################################",
            }),

        new("Valvet",
            "Det stora valvet. Fem vakter, och diamanten bakom en låst valvdörr.\n" +
            "Lycka till, gentleman. Det här är kuppen du blir ihågkommen för.",
            Par: 120,
            Map: new[]
            {
                "####################################",
                "#@    #                      #     #",
                "#  *  #   k              *   #  k  #",
                "#     #                      #     #",
                "##### ########## ############### ###",
                "#>                                 #",
                "#  ##########v ########  ########  #",
                "#  #        #  #      #  #      #  #",
                "#  #   *    D  D  $   D  D  *   #  #",
                "#  #        #  #      #  #      #  #",
                "#  ##########  ######## ^########  #",
                "#                                 <#",
                "###### ################ ############",
                "#    E   #    >          *         #",
                "####################################",
            }),
    };
}
