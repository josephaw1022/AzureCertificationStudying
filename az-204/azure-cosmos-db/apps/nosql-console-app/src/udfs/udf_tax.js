export default {
  id: 'udf_tax',
  body: function udf_tax(price, rate) {
    return Math.round((price * (1 + rate)) * 100) / 100;
  }
};
