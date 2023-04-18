using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace CAServer.MongoDB;

[ConnectionStringName("Default")]
public class CAServerMongoDbContext : AbpMongoDbContext
{
    /* Add mongo collections here. Example:
     * public IMongoCollection<Question> Questions => Collection<Question>();
     */
    //public IMongoCollection<Token> Tokens => Collection<Token>();
    //public IMongoCollection<TokenPriceData> TokenPriceData => Collection<TokenPriceData>();


    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);

        // modelBuilder.Entity<Token>(t =>
        // {
        //     t.CollectionName = CAServerConsts.DbTablePrefix + "Token" + CAServerConsts.DbSchema;
        // });
        // modelBuilder.Entity<TokenPriceData>(t =>
        // {
        //     t.CollectionName = CAServerConsts.DbTablePrefix + "TokenPrice" + CAServerConsts.DbSchema;
        // });
        
    }
}