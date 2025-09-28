export default {
  id: 'sproc_upsert',
  body: function sproc_upsert(doc) {
    var ctx = getContext(), coll = ctx.getCollection();
    var accepted = coll.upsertDocument(coll.getSelfLink(), doc, {}, function (err, d) {
      if (err) throw err; ctx.getResponse().setBody(d);
    });
    if (!accepted) throw new Error('RU limit');
  }
};
