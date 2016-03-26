using System;

using Microsoft.SolverFoundation.Solvers;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics;

namespace UrbanWindPredictorAPI.Models
{
    public static class Learning
    {

        private static double CostFunction(double[] theta, Matrix<Double> featureValues, Vector<Double> trueValues)
        {
            Vector<Double> temptheta = DenseVector.OfArray(theta);
            return Distance.MSE(PredictFunction(featureValues, temptheta), trueValues);
        }

        private static Vector<Double> PredictFunction(Matrix<Double> featureValues, Vector<Double> theta)
        {
            return SigmoidFunction(featureValues * theta);
        }

        private static Vector<Double> SigmoidFunction(Vector<Double> values)
        {
            return (1 / (values.Multiply(-1).PointwiseExp() + 1));
        }

        /// <summary>
        /// Execute optimisation of theta values based on training data.
        /// </summary>
        /// <param name="theta">Current values of theta.</param>
        /// <param name="featureData">values of features.</param>
        /// <param name="result">List of results based on feature values.</param>
        /// <returns>Optimised values of theta</returns>
        public static double[] Learn(double[] theta, double[,]featureData, double[] results)
        {

            double[] xInitial = theta;
            double[] xLower = new double[theta.Length];
            xLower.Populate(-5);
            double[] xUpper = new double[theta.Length];
            xUpper.Populate(5);

            Matrix<Double> featureValues = DenseMatrix.OfArray(featureData);

            Vector<Double> trueValues = DenseVector.OfArray(results);

            var solution = NelderMeadSolver.Solve(x => CostFunction(x, featureValues, trueValues), xInitial, xLower, xUpper);

            double[] optimisedTheta = new double[theta.Length];
            //Console.WriteLine(solution.Result);
            
            for (int i = 0; i < theta.Length; i++)
            {
                optimisedTheta[i] = solution.GetValue(i + 1);
            }

            return optimisedTheta;
        }

    }
}